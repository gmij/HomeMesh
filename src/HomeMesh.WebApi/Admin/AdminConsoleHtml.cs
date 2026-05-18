namespace HomeMesh.WebApi.Admin;

public static class AdminConsoleHtml
{
    public const string Content = """
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1" />
<title>HomeMesh Admin</title>
<style>
body{margin:0;background:#07111f;color:#e6eefc;font-family:system-ui,-apple-system,Segoe UI,sans-serif}.layout{display:grid;grid-template-columns:230px 1fr;min-height:100vh}aside{background:#0b1728;border-right:1px solid #263a56;padding:24px}main{padding:26px}.logo{font-size:24px;font-weight:800;margin-bottom:28px}.nav{color:#8ea4c2;margin:12px 0}.cards{display:grid;grid-template-columns:repeat(4,1fr);gap:14px}.card{background:#102039;border:1px solid #263a56;border-radius:18px;padding:16px;margin-bottom:14px}.metric{font-size:26px;font-weight:800}button,input,select{border-radius:10px;border:1px solid #2d4568;background:#0c1a30;color:#e6eefc;padding:9px 11px}button{background:#0f766e;border:0;font-weight:700;cursor:pointer}table{width:100%;border-collapse:collapse}td,th{padding:10px;border-bottom:1px solid #263a56;text-align:left}.row{display:flex;gap:10px;flex-wrap:wrap;margin:10px 0}.muted{color:#8ea4c2}.log{white-space:pre-wrap;color:#8ea4c2}@media(max-width:900px){.layout{grid-template-columns:1fr}aside{display:none}.cards{grid-template-columns:1fr}}
</style>
</head>
<body>
<div class="layout"><aside><div class="logo">HomeMesh</div><div class="nav">控制台</div><div class="nav">网络</div><div class="nav">成员</div><a class="nav" href="/swagger">Swagger</a></aside><main>
<h1>HomeMesh 管理控制台</h1><p class="muted">Home SD-WAN Controller · ZeroTier Provider</p>
<div class="cards"><div class="card">Health<div id="health" class="metric">-</div></div><div class="card">Providers<div id="providers" class="metric">-</div></div><div class="card">Networks<div id="networkCount" class="metric">0</div></div><div class="card">Members<div id="memberCount" class="metric">0</div></div></div>
<div class="card"><h2>网络</h2><div class="row"><input id="name" placeholder="网络名称"><input id="cidr" placeholder="10.10.0.0/24"><button onclick="createNetwork()">创建</button><button onclick="refreshAll()">刷新</button></div><select id="net" onchange="loadMembers()"></select><table><thead><tr><th>名称</th><th>CIDR</th><th>状态</th></tr></thead><tbody id="netRows"></tbody></table></div>
<div class="card"><h2>配置</h2><div class="row"><input id="route" placeholder="路由CIDR"><button onclick="addRoute()">添加路由</button><input id="poolStart" placeholder="IP池开始"><input id="poolEnd" placeholder="IP池结束"><button onclick="addPool()">添加IP池</button><button onclick="syncConfig()">同步配置</button></div></div>
<div class="card"><h2>成员</h2><button onclick="syncMembers()">同步成员</button><table><thead><tr><th>ID</th><th>名称</th><th>授权</th><th>在线</th></tr></thead><tbody id="memberRows"></tbody></table></div>
<div class="card"><h2>日志</h2><div id="log" class="log">Ready.</div></div>
</main></div>
<script>
let nets=[];const q=id=>document.getElementById(id);const log=m=>q('log').textContent=new Date().toLocaleTimeString()+' '+m+'\n'+q('log').textContent;async function api(p,o){const r=await fetch(p,{headers:{'Content-Type':'application/json'},...o});if(!r.ok)throw new Error(await r.text());return r.status===204?null:await r.json()}async function refreshAll(){try{q('health').textContent=(await api('/health')).status;const ps=await api('/api/providers');q('providers').textContent=ps.map(x=>x.providerName+':'+x.status).join(',')||'-';nets=await api('/api/networks');q('networkCount').textContent=nets.length;q('net').innerHTML=nets.map(n=>`<option value="${n.id}">${n.name}</option>`).join('');q('netRows').innerHTML=nets.map(n=>`<tr><td>${n.name}</td><td>${n.cidr||'-'}</td><td>${n.status}</td></tr>`).join('');await loadMembers();log('刷新完成')}catch(e){log('错误 '+e.message)}}async function createNetwork(){await api('/api/networks',{method:'POST',body:JSON.stringify({name:q('name').value,cidr:q('cidr').value||null,provider:'ZeroTier',private:true})});await refreshAll()}async function loadMembers(){const id=q('net').value;if(!id)return;const ms=await api(`/api/networks/${id}/members`);q('memberCount').textContent=ms.length;q('memberRows').innerHTML=ms.map(m=>`<tr><td>${m.providerMemberId}</td><td>${m.name||'-'}</td><td>${m.authorized?'是':'否'}</td><td>${m.online?'是':'否'}</td></tr>`).join('')}async function addRoute(){const id=q('net').value;await api(`/api/networks/${id}/routes`,{method:'POST',body:JSON.stringify({target:q('route').value})});log('路由已添加')}async function addPool(){const id=q('net').value;await api(`/api/networks/${id}/ip-pools`,{method:'POST',body:JSON.stringify({ipRangeStart:q('poolStart').value,ipRangeEnd:q('poolEnd').value})});log('IP池已添加')}async function syncMembers(){const id=q('net').value;const r=await api(`/api/networks/${id}/members/sync`,{method:'POST'});log('成员同步 '+r.status);await loadMembers()}async function syncConfig(){const id=q('net').value;const r=await api(`/api/networks/${id}/config/sync`,{method:'POST'});log('配置同步 '+r.status)}refreshAll();
</script>
</body></html>
""";
}
