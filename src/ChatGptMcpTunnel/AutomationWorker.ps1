# Windows UI Automation worker. Reads one JSON command from stdin and writes one JSON object to stdout.
# Run under Windows PowerShell 5.1 -STA for compatibility with .NET Framework UIAutomationClient.
$ErrorActionPreference = 'Stop'
try {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\WPF'
    if (-not (Test-Path (Join-Path $framework 'UIAutomationClient.dll'))) {
        $framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\WPF'
    }
    Add-Type -Path (Join-Path $framework 'UIAutomationTypes.dll') -ErrorAction Stop
    Add-Type -Path (Join-Path $framework 'UIAutomationClient.dll') -ErrorAction Stop
    $req = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $walker = [System.Windows.Automation.TreeWalker]::ControlViewWalker
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

    function Describe-Node($node, [string]$path, [int]$depth, [bool]$full) {
        $d = @{
            path=$path
            depth=$depth
            name=(Read-Property $node 'name')
            automation_id=(Read-Property $node 'automation_id')
            type=(Read-Property $node 'type')
            class_name=(Read-Property $node 'class_name')
            bounds=(Read-Property $node 'bounds')
            enabled=(Read-Property $node 'enabled')
            offscreen=(Read-Property $node 'offscreen')
        }
        if ($full) {
            $d.framework_id = Read-Property $node 'framework_id'
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
        }
        return $d
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
        $result = New-Object System.Collections.ArrayList
        $child = $walker.GetFirstChild($node)
        while ($null -ne $child -and $result.Count -lt 1000) {
            [void]$result.Add($child)
            $child = $walker.GetNextSibling($child)
        }
        return ,($result.ToArray())
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
            $output = @{ hwnd=$req.hwnd;count=$nodes.Count;truncated=($pending.Count -gt 0);elements=@($nodes.ToArray());max_depth=$maxDepth }
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
                $type = Read-Property $item.element 'type'
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
            $output = @{hwnd=$req.hwnd;matches=@($nodes.ToArray());count=$nodes.Count;scanned=$visited;truncated=($pending.Count -gt 0)}
        }
        'get' {
            $node = Resolve-Path $root ([string]$req.path)
            $output = Describe-Node $node ([string]$req.path) 0 $true
        }
        'action' {
            $node = Resolve-Path $root ([string]$req.path)
            $action = [string]$req.action
            switch ($action) {
                'invoke' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.InvokePattern]::Pattern)
                    if ($null -eq $pattern) { throw 'This element does not support InvokePattern.' }
                    $pattern.Invoke()
                }
                'set_value' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.ValuePattern]::Pattern)
                    if ($null -eq $pattern) { throw 'This element does not support ValuePattern.' }
                    $pattern.SetValue([string]$req.value)
                }
                'select' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.SelectionItemPattern]::Pattern)
                    if ($null -eq $pattern) { throw 'This element does not support SelectionItemPattern.' }
                    $pattern.Select()
                }
                'toggle' {
                    $pattern = Get-Pattern $node ([System.Windows.Automation.TogglePattern]::Pattern)
                    if ($null -eq $pattern) { throw 'This element does not support TogglePattern.' }
                    $pattern.Toggle()
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
            $output = @{hwnd=$req.hwnd;path=$req.path;action=$action;success=$true}
        }
        default { throw ('Unknown UIA operation: '+$req.operation) }
    }
    [Console]::Out.WriteLine(($output | ConvertTo-Json -Depth 12 -Compress))
} catch {
    [Console]::Out.WriteLine((@{success=$false;error=$_.Exception.Message;error_type=$_.Exception.GetType().Name} | ConvertTo-Json -Compress))
    exit 1
}
