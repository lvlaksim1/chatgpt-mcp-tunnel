# Windows UI Automation worker. Reads one JSON command from stdin and writes one JSON object to stdout.
# Run under Windows PowerShell 5.1 -STA for compatibility with .NET Framework UIAutomationClient.
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = New-Object System.Text.UTF8Encoding($false)
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding($false)
try {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
    if (-not (Test-Path (Join-Path $framework 'UIAutomationClient.dll'))) {
        $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\WPF'
    }
    Add-Type -Path (Join-Path $framework 'UIAutomationTypes.dll') -ErrorAction Stop
    Add-Type -Path (Join-Path $framework 'UIAutomationClient.dll') -ErrorAction Stop
    # Default UIA client-side proxies are installed with Windows. Explicit registration
    # may throw on some Windows 10 installations; do not make basic UIA dependent on it.
    $proxyAssembly = Join-Path $framework 'UIAutomationClientsideProviders.dll'
    $providers = @{
        assembly_present = (Test-Path -LiteralPath $proxyAssembly)
        registration = 'Windows default registration'
        legacy_pattern_api = 'Not exposed by .NET Framework UIAutomationClient; MSAA inspected separately'
    }
    Add-Type -AssemblyName Accessibility -ErrorAction Stop
    Add-Type -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot 'MsaaBridge.cs.txt'))) -ReferencedAssemblies Accessibility -ErrorAction Stop
    while ($null -ne ($inputLine = [Console]::In.ReadLine())) {
    try {
    $req = $inputLine | ConvertFrom-Json
    $view = if ($null -eq $req.view -or [string]$req.view -eq '') { 'control' } else { [string]$req.view }
    if ($view -notin @('control','raw')) { throw 'view must be control or raw' }
    $walker = if ($view -eq 'raw') { [System.Windows.Automation.TreeWalker]::RawViewWalker } else { [System.Windows.Automation.TreeWalker]::ControlViewWalker }
    $root = [System.Windows.Automation.AutomationElement]::FromHandle([IntPtr]([Convert]::ToInt64(([string]$req.hwnd).Replace('0x',''),16)))
    if ($null -eq $root) { throw 'Window handle is invalid or inaccessible.' }

    function Read-Property($node, [string]$property) {
        try {
            $current = $node.Current
            switch ($property) {
                'name' { return [string]$current.Name }
                'automation_id' { return [string]$current.AutomationId }
                'type' { return ([string]$current.ControlType.ProgrammaticName).Replace('ControlType.','') }
                'class_name' { return [string]$current.ClassName }
                'framework_id' { return [string]$current.FrameworkId }
                'enabled' { return [bool]$current.IsEnabled }
                'offscreen' { return [bool]$current.IsOffscreen }
                'focusable' { return [bool]$current.IsKeyboardFocusable }
                'focused' { return [bool]$current.HasKeyboardFocus }
                'native_hwnd' { return [int]$current.NativeWindowHandle }
                'bounds' {
                    $b = $current.BoundingRectangle
                    if ($b.IsEmpty) { return $null }
                    return @{x=[math]::Round($b.X);y=[math]::Round($b.Y);width=[math]::Round($b.Width);height=[math]::Round($b.Height)}
                }
            }
        } catch { return $null }
        return $null
    }

    function Get-Pattern($node, $patternIdentifier) {
        try { return $node.GetCurrentPattern($patternIdentifier) } catch { return $null }
    }

    function Effective-Type($node) {
        $reported = Read-Property $node 'type'
        $classification = @{type=$reported;uia_type=$reported;type_source='uia'}
        if ($reported -ne 'Pane') { return $classification }
        $className = Read-Property $node 'class_name'
        if (-not $className.StartsWith('WindowsForms10.')) { return $classification }
        $native = Read-Property $node 'native_hwnd'
        if (-not $native) { return $classification }
        $info = [MsaaBridge]::Inspect([long]$native)
        if (-not $info.available -or -not $info.ContainsKey('role')) { return $classification }
        $mapped = switch ([int]$info.role) {
            42 { 'Edit' }
            43 { 'Button' }
            44 { 'CheckBox' }
            45 { 'RadioButton' }
            46 { 'ComboBox' }
            34 { 'List' }
            default { $null }
        }
        if ($mapped) {
            $classification.type = $mapped
            $classification.type_source = 'msaa'
        }
        return $classification
    }

    function Describe-Node($node, [string]$path, [int]$depth, [bool]$full) {
        $classification = Effective-Type $node
        $d = @{
            path=$path
            depth=$depth
            name=(Read-Property $node 'name')
            automation_id=(Read-Property $node 'automation_id')
            type=$classification.type
            uia_type=$classification.uia_type
            type_source=$classification.type_source
            class_name=(Read-Property $node 'class_name')
            bounds=(Read-Property $node 'bounds')
            enabled=(Read-Property $node 'enabled')
            offscreen=(Read-Property $node 'offscreen')
        }
        if ($full) {
            $d.framework_id = Read-Property $node 'framework_id'
            $d.client_side_providers = $providers
            $d.view = $view
            $hwnd = Read-Property $node 'native_hwnd'
            $d.native_hwnd = $hwnd
            $d.legacy_iaccessible = @{managed_pattern_supported=$false;note='LegacyIAccessiblePattern not exposed by managed UIA; inspect msaa below'}
            $d.msaa = [MsaaBridge]::Inspect([long]$hwnd)
            $d.focused = Read-Property $node 'focused'
            $d.focusable = Read-Property $node 'focusable'
            $patterns = New-Object System.Collections.Generic.List[string]
            $valuePattern = Get-Pattern $node ([System.Windows.Automation.ValuePattern]::Pattern)
            if ($null -ne $valuePattern) {
                $patterns.Add('value')
                try {
                    $v=[string]$valuePattern.Current.Value
                    $d.value = if ($v.Length -gt 12000) { $v.Substring(0,12000) } else { $v }
                    $d.value_readonly = [bool]$valuePattern.Current.IsReadOnly
                } catch {}
            }
            $textPattern = Get-Pattern $node ([System.Windows.Automation.TextPattern]::Pattern)
            if ($null -ne $textPattern) {
                $patterns.Add('text')
                try { $d.text = $textPattern.DocumentRange.GetText(12000) } catch {}
            }
            $invoke = Get-Pattern $node ([System.Windows.Automation.InvokePattern]::Pattern)
            if ($null -ne $invoke) { $patterns.Add('invoke') }
            $select = Get-Pattern $node ([System.Windows.Automation.SelectionItemPattern]::Pattern)
            if ($null -ne $select) {
                $patterns.Add('select')
                try { $d.selected = [bool]$select.Current.IsSelected } catch {}
            }
            $toggle = Get-Pattern $node ([System.Windows.Automation.TogglePattern]::Pattern)
            if ($null -ne $toggle) {
                $patterns.Add('toggle')
                try { $d.toggle_state = [string]$toggle.Current.ToggleState } catch {}
            }
            $expand = Get-Pattern $node ([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
            if ($null -ne $expand) {
                $patterns.Add('expand_collapse')
                try { $d.expand_state = [string]$expand.Current.ExpandCollapseState } catch {}
            }
            $scrollItem = Get-Pattern $node ([System.Windows.Automation.ScrollItemPattern]::Pattern)
            if ($null -ne $scrollItem) { $patterns.Add('scroll_into_view') }
            $d.patterns = @($patterns.ToArray())
            $availableActions = New-Object System.Collections.ArrayList
            foreach ($name in $patterns) {
                switch ($name) {
                    'invoke' { [void]$availableActions.Add(@{action='invoke';backend='uia'}) }
                    'value' { if (-not $d.value_readonly) { [void]$availableActions.Add(@{action='set_value';backend='uia'}) } }
                    'select' { [void]$availableActions.Add(@{action='select';backend='uia'}) }
                    'toggle' { [void]$availableActions.Add(@{action='toggle';backend='uia'}) }
                    'expand_collapse' {
                        [void]$availableActions.Add(@{action='expand';backend='uia'})
                        [void]$availableActions.Add(@{action='collapse';backend='uia'})
                    }
                    'scroll_into_view' { [void]$availableActions.Add(@{action='scroll_into_view';backend='uia'}) }
                }
            }
            if ($d.focusable) { [void]$availableActions.Add(@{action='focus';backend='uia'}) }
            if ($d.msaa.available) {
                if ($d.msaa.default_action) {
                    if (-not ($patterns -contains 'invoke')) { [void]$availableActions.Add(@{action='invoke';backend='msaa'}) }
                    if ($d.msaa.role -eq 44 -and -not ($patterns -contains 'toggle')) {
                        [void]$availableActions.Add(@{action='toggle';backend='msaa'})
                    }
                }
                if ($d.msaa.role -eq 42 -and -not ($patterns -contains 'value') -and
                    (($d.msaa.state -band 64) -eq 0)) {
                    [void]$availableActions.Add(@{action='set_value';backend='msaa'})
                }
            }
            $d.available_actions = @($availableActions.ToArray())
        }
        return $d
    }

    function Invoke-MsaaFallback($node, [string]$action, [string]$value) {
        $native = Read-Property $node 'native_hwnd'
        if ([long]$native -le 0) { throw "No native HWND for MSAA fallback ($action)." }
        $inspection = [MsaaBridge]::Inspect([long]$native)
        if (-not $inspection.available) { throw ("MSAA fallback not available: "+$inspection.reason) }
        if ($action -eq 'toggle' -and $inspection.role -ne 44) {
            throw 'MSAA toggle fallback requires a checkbox role.'
        }
        if ($action -in @('invoke','toggle') -and -not $inspection.default_action) {
            throw 'MSAA provider does not advertise a default action.'
        }
        return [MsaaBridge]::Act([long]$native, $action, $value)
    }

    function Resolve-Path($origin, [string]$path) {
        $current = $origin
        if ([string]::IsNullOrWhiteSpace($path)) { return $current }
        foreach ($step in $path.Split('/')) {
            if ($step -notmatch '^\d+$') { throw ('Invalid element path: '+$path) }
            $index = [int]$step
            if ($index -gt 10000) { throw 'Element path index is too large.' }
            $child = $walker.GetFirstChild($current)
            for ($i=0; $i -lt $index; $i++) {
                if ($null -eq $child) { break }
                $child = $walker.GetNextSibling($child)
            }
            if ($null -eq $child) { throw ('Element path no longer exists: '+$path) }
            $current = $child
        }
        return $current
    }

    function Get-Children($node) {
        if ($null -eq $node) { throw 'UIA_DEBUG: node is null' }
        if ($node -isnot [System.Windows.Automation.AutomationElement]) {
            throw ('UIA_DEBUG: unexpected node type '+$node.GetType().FullName)
        }
        if ($null -eq $walker) { throw 'UIA_DEBUG: walker is null' }
        $result = New-Object System.Collections.ArrayList
        $child = $walker.GetFirstChild($node)
        while ($null -ne $child -and $result.Count -lt 1000) {
            [void]$result.Add($child)
            $child = $walker.GetNextSibling($child)
        }
        return $result.ToArray()
    }

    switch ([string]$req.operation) {
        'tree' {
            $limit = [math]::Min([math]::Max([int]$req.max_results,1),500)
            $maxDepth = [math]::Min([math]::Max([int]$req.max_depth,0),12)
            $nodes = New-Object System.Collections.ArrayList
            $pending = New-Object System.Collections.Stack
            $pending.Push(@{element=$root;path='';depth=0})
            $visited = 0
            while ($pending.Count -gt 0 -and $nodes.Count -lt $limit) {
                $item = $pending.Pop()
                $visited++
                [void]$nodes.Add((Describe-Node $item.element $item.path $item.depth $false))
                if ($item.depth -ge $maxDepth) { continue }
                $children = @(Get-Children $item.element)
                for ($i=$children.Count-1;$i -ge 0;$i--) {
                    $path = if ($item.path -eq '') { [string]$i } else { $item.path+'/'+$i }
                    $pending.Push(@{element=$children[$i];path=$path;depth=($item.depth+1)})
                }
            }
            $output = @{ hwnd=$req.hwnd;view=$view;count=$nodes.Count;truncated=($pending.Count -gt 0);elements=@($nodes.ToArray());max_depth=$maxDepth }
        }
        'find' {
            $limit = [math]::Min([math]::Max([int]$req.max_results,1),100)
            $maxDepth = [math]::Min([math]::Max([int]$req.max_depth,0),20)
            $nodes = New-Object System.Collections.ArrayList
            $pending = New-Object System.Collections.Stack
            $pending.Push(@{element=$root;path='';depth=0})
            $visited = 0
            $scannedLimit = 3500
            while ($pending.Count -gt 0 -and $nodes.Count -lt $limit -and $visited -lt $scannedLimit) {
                $item = $pending.Pop()
                $visited++
                $name = Read-Property $item.element 'name'
                $automationId = Read-Property $item.element 'automation_id'
                $type = (Effective-Type $item.element).type
                $className = Read-Property $item.element 'class_name'
                $match = $true
                if ($req.name -and ($null -eq $name -or $name.IndexOf([string]$req.name,[StringComparison]::OrdinalIgnoreCase) -lt 0)) { $match = $false }
                if ($req.automation_id -and $automationId -ne [string]$req.automation_id) { $match = $false }
                if ($req.control_type -and $type -ne [string]$req.control_type) { $match = $false }
                if ($req.class_name -and $className -ne [string]$req.class_name) { $match = $false }
                if ($match) { [void]$nodes.Add((Describe-Node $item.element $item.path $item.depth $false)) }
                if ($item.depth -ge $maxDepth) { continue }
                $children = @(Get-Children $item.element)
                for ($i=$children.Count-1;$i -ge 0;$i--) {
                    $path = if ($item.path -eq '') { [string]$i } else { $item.path+'/'+$i }
                    $pending.Push(@{element=$children[$i];path=$path;depth=($item.depth+1)})
                }
            }
            $output = @{hwnd=$req.hwnd;view=$view;matches=@($nodes.ToArray());count=$nodes.Count;scanned=$visited;truncated=($pending.Count -gt 0)}
        }
        'get' {
            $node = Resolve-Path $root ([string]$req.path)
            $output = Describe-Node $node ([string]$req.path) 0 $true
        }
        'action' {
            $node = Resolve-Path $root ([string]$req.path)
            $action = [string]$req.action
            $backend = 'uia'
            switch ($action) {
                'invoke' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.InvokePattern]::Pattern)
                    if ($null -eq $pattern) { $backend = Invoke-MsaaFallback $node 'invoke' '' }
                    else { $pattern.Invoke() }
                }
                'set_value' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.ValuePattern]::Pattern)
                    if ($null -eq $pattern) { $backend = Invoke-MsaaFallback $node 'set_value' ([string]$req.value) }
                    else { $pattern.SetValue([string]$req.value) }
                }
                'select' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.SelectionItemPattern]::Pattern)
                    if ($null -eq $pattern) { $backend = Invoke-MsaaFallback $node 'select' '' }
                    else { $pattern.Select() }
                }
                'toggle' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.TogglePattern]::Pattern)
                    if ($null -eq $pattern) { $backend = Invoke-MsaaFallback $node 'toggle' '' }
                    else { $pattern.Toggle() }
                }
                'expand' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                    if ($null -eq $pattern) { throw 'This element does not support ExpandCollapsePattern.' }
                    $pattern.Expand()
                }
                'collapse' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
                    if ($null -eq $pattern) { throw 'This element does not support ExpandCollapsePattern.' }
                    $pattern.Collapse()
                }
                'scroll_into_view' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.ScrollItemPattern]::Pattern)
                    if ($null -eq $pattern) { throw 'This element does not support ScrollItemPattern.' }
                    $pattern.ScrollIntoView()
                }
                'focus' { $node.SetFocus() }
                default { throw ('Unsupported UIA action: '+$action) }
            }
            $output = @{hwnd=$req.hwnd;path=$req.path;view=$view;action=$action;backend=$backend;success=$true}
        }
        default { throw ('Unknown UIA operation: '+$req.operation) }
    }
    [Console]::Out.WriteLine(($output | ConvertTo-Json -Depth 12 -Compress))
    [Console]::Out.Flush()
    } catch {
        [Console]::Out.WriteLine((@{success=$false;error=$_.Exception.Message;error_type=$_.Exception.GetType().Name} | ConvertTo-Json -Compress))
        [Console]::Out.Flush()
    }
    }
} catch {
    [Console]::Out.WriteLine((@{success=$false;error=$_.Exception.Message;error_type=$_.Exception.GetType().Name} | ConvertTo-Json -Compress))
    exit 1
}
