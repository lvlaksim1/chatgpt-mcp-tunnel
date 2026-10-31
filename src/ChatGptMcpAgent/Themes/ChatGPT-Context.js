/* Context menus for clickable ChatGPT content. The host owns tabs.
 * This code never sends messages to ChatGPT or reads private chat content.
 */
(() => {
  'use strict';
  if (!window.chrome?.webview?.postMessage) return;
  if (window.__mcpClickableContext?.stop) window.__mcpClickableContext.stop();

  function locate(target) {
    if (!(target instanceof Element)) return null;
    let el = target.closest('a[href],button,[role="link"],[role="button"],[onclick],summary,[tabindex],.cursor-pointer,.cursor-interaction');
    if (!el) {
      const style = getComputedStyle(target);
      if (style.cursor === 'pointer') el = target;
    }
    if (!el || el.matches('input,textarea,[contenteditable="true"],select')) return null;
    return el;
  }
  function selectorOf(el) {
    if (el.id) return '#' + CSS.escape(el.id);
    const test = el.getAttribute('data-testid');
    if (test) {
      const q='[data-testid="'+CSS.escape(test)+'"]';
      if(document.querySelectorAll(q).length === 1) return q;
    }
    const aria = el.getAttribute('aria-label');
    if (aria && !aria.includes('"')) {
      const q=el.tagName.toLowerCase()+'[aria-label="'+aria+'"]';
      if(document.querySelectorAll(q).length === 1) return q;
    }
    const result=[];
    for(let node=el;node&&node!==document.body;node=node.parentElement){
      if (!node.parentElement) return '';
      const tag=node.tagName.toLowerCase();
      const siblings=[...node.parentElement.children].filter(x=>x.tagName === node.tagName);
      result.unshift(tag+':nth-of-type('+(siblings.indexOf(node)+1)+')');
    }
    return 'body>'+result.join('>');
  }

  function onContext(e) {
    if (!e.isTrusted) return;
    const clickable=locate(e.target);
    if (!clickable) return; // Preserve native menu on plain text and inputs.
    let rawUrl=clickable.closest('a[href]')?.getAttribute('href') ||
      clickable.getAttribute('href') || clickable.getAttribute('data-href') || '';
    let uri='';
    if(rawUrl){
      try {
        const u=new URL(rawUrl, document.baseURI);
        if(['https:','http:'].includes(u.protocol)) uri=u.href;
      } catch(_){}
    }
    const selector=selectorOf(clickable);
    if(!uri && !selector) return;
    e.preventDefault();
    e.stopPropagation();
    e.stopImmediatePropagation();
    window.chrome.webview.postMessage({
      mcp_kind:'clickable_context',
      x:Math.round(e.clientX),
      y:Math.round(e.clientY),
      href:uri,
      selector
    });
  }
  function onKeydown(e) {
    const ctrl = e.ctrlKey || e.metaKey;
    const shortcut = ctrl && (
      e.key === 'F5' ? 'hard_reload' :
      e.key.toLowerCase() === 't' ? 'new_tab' :
      e.key.toLowerCase() === 'w' ? 'close_tab' :
      e.key.toLowerCase() === 'l' ? 'focus_address' :
      e.key === 'Tab' ? (e.shiftKey ? 'previous_tab' : 'next_tab') : '');
    if (!shortcut) return;
    e.preventDefault();
    e.stopImmediatePropagation();
    window.chrome.webview.postMessage({mcp_kind:'browser_shortcut', action:shortcut});
  }
  window.addEventListener('keydown',onKeydown,true);
  document.addEventListener('contextmenu',onContext,true);
  window.__mcpClickableContext={stop(){
    document.removeEventListener('contextmenu',onContext,true);
    window.removeEventListener('keydown',onKeydown,true);
  }};
})();
