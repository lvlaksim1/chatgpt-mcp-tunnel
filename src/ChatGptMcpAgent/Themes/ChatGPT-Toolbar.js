(() => {
  'use strict';
  if(location.hostname!=='chatgpt.com') return;
  window.__mcpConversationToolbar?.stop?.();
  let actions,share,timer=0,busy=false;
  const changes=new Map();
  const text=n=>(n?.getAttribute('aria-label')||n?.getAttribute('title')||n?.textContent||'').trim();
  function set(el,prop,value){
    if(!el)return;
    if(!changes.has(el))changes.set(el,new Map());
    if(!changes.get(el).has(prop))changes.get(el).set(prop,[el.style.getPropertyValue(prop),el.style.getPropertyPriority(prop)]);
    if(el.style.getPropertyValue(prop)!==value||el.style.getPropertyPriority(prop)!=='important')
      el.style.setProperty(prop,value,'important');
  }
  function locate(){
    const buttons=[...document.querySelectorAll('button')];
    actions=buttons.find(b=>text(b)==='Chat actions'&&b.getBoundingClientRect().width>0&&b.getBoundingClientRect().top<140) || actions;
    if(!actions?.isConnected)return;
    const a=actions.getBoundingClientRect();
    share=buttons.find(b=>b!==actions&&['Поделиться','Share'].includes(text(b))&&
      b.getBoundingClientRect().width>0&&
      Math.abs(b.getBoundingClientRect().top-a.top)<25&&
      b.getBoundingClientRect().left>=a.left)||share;
  }
  function paint(){
    if(!actions?.isConnected)return;
    if(actions.getAttribute('title')==='MCP-TEST-APPLIED') actions.removeAttribute('title');
    if(share?.isConnected)set(share,'display','none');
    set(actions,'margin-inline-start','auto');
    set(actions,'background-color','#393934');
    set(actions,'color','#E5D9BF');
    set(actions,'border','1px solid #505049');
    set(actions,'border-radius','10px');
    const parent=actions.parentElement;
    if(parent){set(parent,'justify-content','flex-end');set(parent,'gap','8px')}
    actions.classList.add('mcp-toolbar-actions');
    if(!document.getElementById('mcp-toolbar-style')){
      const style=document.createElement('style');
      style.id='mcp-toolbar-style';
      style.textContent=[
        'button.mcp-toolbar-actions{background:#393934!important;color:#E5D9BF!important;}',
        'button.mcp-toolbar-actions:hover{background:#505049!important;}',
        'button.mcp-toolbar-actions:active{background:#444439!important;}',
        'button.mcp-toolbar-actions svg{color:#E5D9BF!important;}',
        '[data-mcp-share-action="true"]:hover,[data-mcp-share-action="true"]:focus-visible{background:#393934!important;}'
      ].join('\n');
      document.head.append(style);
    }
  }
  function showShare(){
    const original=share?.isConnected?share:null;
    if(original){original.click();return true}
    return false;
  }
  function addMenu(){
    if(!actions?.isConnected)return;
    const rename=[...document.querySelectorAll('[role="menuitem"]')].find(n=>
      /Переименовать|Rename/.test(n.textContent||''));
    if(!rename)return;
    const menu=rename.closest('[role="menu"]')||rename.closest('[data-radix-menu-content]')||rename.parentElement;
    if(!menu?.isConnected||menu.querySelector('[data-mcp-share-action="true"]'))return;
    if([...menu.querySelectorAll('[role="menuitem"]')].some(n=>['Поделиться','Share'].includes(text(n))))return;
    const item=rename.cloneNode(true);
    for(const name of ['id','data-state','aria-haspopup','aria-expanded','aria-labelledby','aria-describedby'])item.removeAttribute(name);
    item.setAttribute('data-mcp-share-action','true');
    item.setAttribute('aria-label','Поделиться');
    item.setAttribute('tabindex','0');
    let translated=false;
    const walker=document.createTreeWalker(item,NodeFilter.SHOW_TEXT);
    for(let node;(node=walker.nextNode());){
      if(/Переименовать|Rename/.test(node.nodeValue||'')){
        node.nodeValue=node.nodeValue.replace(/Переименовать|Rename/,'Поделиться');
        translated=true;
      }
    }
    if(!translated)item.textContent='Поделиться';
    // ChatGPT's native menu icons use CSS mask-image on a span, not SVG.
    // Reuse the existing icon slot and replace only its shape.
    const glyph=item.querySelector('[data-leading-icon-slot="menu"] [style*="mask-image"]');
    if(glyph){
      const svg='<svg xmlns="http://www.w3.org/2000/svg" width="21" height="21" viewBox="0 0 21 21" fill="none" stroke="black" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"><path d="M10.5 13.7V2.7m0 0-4 4m4-4 4 4"/><path d="M4.2 11.7v5.5c0 .7.5 1.2 1.2 1.2h10.2c.7 0 1.2-.5 1.2-1.2v-5.5"/></svg>';
      glyph.style.setProperty('mask-image','url("data:image/svg+xml,'+encodeURIComponent(svg)+'")','important');
    }
    const activate=event=>{
      event.preventDefault();
      event.stopImmediatePropagation();
      if(showShare()) setTimeout(()=>{
        // Dismiss the original overflow menu after invoking native sharing.
        if(actions?.isConnected&&actions.getAttribute('aria-expanded')==='true')
          actions.click();
      },0);
    };
    item.addEventListener('click',activate);
    item.addEventListener('keydown',e=>{if(e.key==='Enter'||e.key===' '){activate(e)}});
    rename.insertAdjacentElement('afterend',item);
  }
  function apply(){
    if(busy||!document.body)return;
    busy=true;
    try{locate();paint();addMenu()}catch(_){}
    busy=false;
  }
  function schedule(){if(!timer)timer=setTimeout(()=>{timer=0;apply()},110)}
  const observer=new MutationObserver(schedule);
  window.__mcpConversationToolbar={
    stop(){
      observer.disconnect();if(timer)clearTimeout(timer);
      for(const [node,props] of changes){
        for(const [prop,[value,priority]] of props){
          if(value)node.style.setProperty(prop,value,priority);
          else node.style.removeProperty(prop);
        }
      }
      actions?.classList.remove('mcp-toolbar-actions');
      document.getElementById('mcp-toolbar-style')?.remove();
      document.querySelectorAll('[data-mcp-share-action="true"]').forEach(x=>x.remove());
      changes.clear();
    }
  };
  function start(){if(!document.body){document.addEventListener('DOMContentLoaded',start,{once:true});return}
    observer.observe(document.body,{subtree:true,childList:true});apply()}
  start();
})();
