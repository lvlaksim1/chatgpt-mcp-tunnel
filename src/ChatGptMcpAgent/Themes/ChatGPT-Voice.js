/* ChatGPT MCP Tunnel — local voice bridge.
 * Only operates on chatgpt.com; no API keys, cookies, storage, or network.
 * Insertion uses the existing composer. Cloud chat content stays on the page.
 */
(() => {
  'use strict';
  if (location.hostname !== 'chatgpt.com') return;
  window.__mcpVoiceBridge?.dispose?.();

  let enabled = false;
  let lastMessage = null;
  let lastText = '';
  let reportedText = '';
  let generationTimer = 0;
  let armedSince = Date.now();

  function send(kind, details) {
    try { window.chrome?.webview?.postMessage({mcp_kind:kind,...details}); }
    catch (_) {}
  }
  function assistantNodes() {
    // Handle both the legacy transcript and the current DIL response renderer.
    const nodes=[...document.querySelectorAll(
      '[data-message-author-role="assistant"],[class*="DilResponseRoot"]')];
    return nodes.filter(node=>!node.matches('[class*="DilResponseRoot"]') ||
      !node.closest('[data-message-author-role="assistant"]'));
  }
  function lastAssistant() {
    const nodes = assistantNodes();
    return nodes.length ? nodes[nodes.length-1] : null;
  }
  function replyText(element) {
    const body = element?.querySelector('.markdown,.prose,[data-testid="conversation-turn-content"]');
    const text=(body||element)?.innerText||'';
    return text.replace(/\u00a0/g,' ').trim().slice(0,3500);
  }
  function isGenerating() {
    return !!document.querySelector(
      '[data-testid="stop-button"],button[aria-label="Остановить генерацию"],button[aria-label="Stop generating"]');
  }
  function inspectReply() {
    if (!enabled) return;
    const node = lastAssistant();
    if (!node) return;
    const text = replyText(node);
    if (!text) return;
    if (node!==lastMessage) {
      lastMessage=node;
      lastText=text;
      reportedText='';
      // A loaded older assistant message is not a fresh reply.
      if(Date.now()-armedSince<1000) return;
    }
    if (text!==lastText) {
      lastText=text;
      clearTimeout(generationTimer);
    }
    if (isGenerating()) return;
    if (reportedText===text) return;
    clearTimeout(generationTimer);
    generationTimer=setTimeout(()=>{
      if (!enabled || isGenerating() || lastAssistant()!==node) return;
      const current=replyText(node);
      if(!current || current!==text || reportedText===current)return;
      reportedText=current;
      send('voice_reply',{text:current,url:location.href});
    },1000);
  }
  const observer = new MutationObserver(inspectReply);
  function start() {
    if (!document.body) {
      document.addEventListener('DOMContentLoaded',start,{once:true});return;
    }
    lastMessage=lastAssistant();
    lastText=replyText(lastMessage);
    reportedText=lastText;
    observer.observe(document.body,{subtree:true,childList:true,characterData:true});
  }

  function setComposer(value, submit) {
    if(typeof value!=='string'||value.length>15000)return false;
    const editor=document.querySelector(
      '#prompt-textarea[contenteditable="true"],#prompt-textarea,[data-testid="composer"] [contenteditable="true"]');
    if(!editor || editor.closest('[aria-hidden="true"]'))return false;
    editor.focus();
    if(editor.isContentEditable){
      const selection=window.getSelection();
      const range=document.createRange();
      range.selectNodeContents(editor);
      selection?.removeAllRanges();
      selection?.addRange(range);
      // Use browser-native editing (ProseMirror/Tiptap observes the
      // beforeinput/input sequence) instead of assigning innerHTML.
      document.execCommand('insertText',false,value);
    } else {
      const proto=editor instanceof HTMLTextAreaElement?
        HTMLTextAreaElement.prototype:HTMLInputElement.prototype;
      Object.getOwnPropertyDescriptor(proto,'value')?.set?.call(editor,value);
      editor.dispatchEvent(new Event('input',{bubbles:true}));
    }
    editor.dispatchEvent(new InputEvent('input',{bubbles:true,inputType:'insertText',data:value}));
    if(submit){
      setTimeout(()=>{
        if(document.activeElement!==editor) return;
        const form=editor.closest('form')||document.querySelector('[data-testid="composer"]');
        const button=form?.querySelector(
          'button[data-testid="send-button"],button[aria-label*="Отправ"],button[aria-label*="Send"]');
        if(button && !button.disabled && button.getAttribute('aria-disabled')!=='true')
          button.click();
      },250);
    }
    return true;
  }
  window.__mcpVoiceBridge = {
    setEnabled(value) {
      enabled=!!value;
      lastMessage=lastAssistant();
      lastText=replyText(lastMessage);
      reportedText=lastText;
      armedSince=Date.now();
    },
    setComposer,
    dispose() {observer.disconnect();clearTimeout(generationTimer);}
  };
  start();
})();
