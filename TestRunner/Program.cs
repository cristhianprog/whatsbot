using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using WASender;

namespace TestRunner
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            string profile = args.Length > 0 ? args[0] : @"C:\ProgramData\WaSender\Profiles\EBWebView";
            string target = args.Length > 1 ? args[1] : "5551985719297";
            string csvPath = args.Length > 2 ? args[2] : @"C:\ProgramData\WaSender\SysFiles\chatTemplate.csv";
            string logPath = args.Length > 3 ? args[3] : @"E:\01 - Negocios\WaSender\WhatsBot\teste-envio-resultado.txt";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new RunnerForm(profile, target, csvPath, logPath));
        }
    }

    public class RunnerForm : Form
    {
        WebView2 wv;
        string profile, target, csvPath, logPath;
        StringBuilder log = new StringBuilder();
        System.Windows.Forms.Timer poll;
        int stage = 0;
        int ticks = 0;
        int reinjections = 0;
        string csv;

        public RunnerForm(string profile, string target, string csvPath, string logPath)
        {
            this.profile = profile;
            this.target = target;
            if (target.Contains("@") == false)
            {
                this.sendJid = target + "@c.us";
            }
            else
            {
                this.sendJid = target;
            }
            this.csvPath = csvPath;
            this.logPath = logPath;
            this.Text = "TestRunner WhatsBot";
            this.Width = 500;
            this.Height = 400;
            this.Load += RunnerForm_Load;
        }

        string sendJid;

        void Log(string s)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + " " + s;
            log.AppendLine(line);
            try { File.AppendAllText(logPath, line + "\r\n", Encoding.UTF8); } catch { }
            System.Diagnostics.Debug.WriteLine(line);
        }

        async void RunnerForm_Load(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText(logPath, "=== Teste iniciado " + DateTime.Now + " ===\r\n", Encoding.UTF8);
                string consoleHook = "(function(){function fw(t){try{var a=[];for(var i=0;i<arguments.length;i++){a.push(String(arguments[i]).substring(0,400));}chrome.webview.postMessage('__console '+t+' '+a.join(' | '));}catch(e){}}var ol=console.log,ow=console.warn,oe=console.error;console.log=function(){fw('LOG');ol.apply(console,arguments);};console.warn=function(){fw('WARN');ow.apply(console,arguments);};console.error=function(){fw('ERROR');oe.apply(console,arguments);};})();";
                csv = consoleHook + File.ReadAllText(csvPath);
                Log("CSV carregado: " + csv.Length + " bytes");
                wv = new WebView2();
                wv.Dock = DockStyle.Fill;
                wv.CreationProperties = new CoreWebView2CreationProperties();
                wv.CreationProperties.UserDataFolder = profile;
                Controls.Add(wv);
                await wv.EnsureCoreWebView2Async(null);
                Log("WebView2 inicializado com perfil: " + profile);
                wv.CoreWebView2.WebMessageReceived += (s2, ev) =>
                {
                    try
                    {
                        string msg = ev.TryGetWebMessageAsString();
                        if (msg != null && msg.Contains("__console"))
                        {
                            Log("[console] " + msg);
                        }
                    }
                    catch { }
                };
                wv.CoreWebView2.NavigationCompleted += (s3, ev3) =>
                {
                    try { Log("navegacao concluida: " + (ev3.IsSuccess ? "ok" : "falha")); } catch { }
                };
                wv.CoreWebView2.Navigate("https://web.whatsapp.com/");
                Log("Navegando para web.whatsapp.com...");
                poll = new System.Windows.Forms.Timer();
                poll.Interval = 2000;
                poll.Tick += PollTick;
                poll.Start();
            }
            catch (Exception ex)
            {
                Log("ERRO FATAL init: " + ex.Message);
                Finish();
            }
        }

        async System.Threading.Tasks.Task<string> EvalJS(string js)
        {
            string res = await wv.ExecuteScriptAsync(js);
            if (string.IsNullOrEmpty(res) || res == "null") { return null; }
            try { return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(res); }
            catch { return res; }
        }

        async System.Threading.Tasks.Task<string> RunAsyncJS(string jsCall, int maxPolls, int pollMs)
        {
            await wv.ExecuteScriptAsync(jsCall);
            for (int i = 0; i < maxPolls; i++)
            {
                await System.Threading.Tasks.Task.Delay(pollMs);
                string res = await wv.ExecuteScriptAsync("window.__t");
                string val = null;
                try { val = Newtonsoft.Json.JsonConvert.DeserializeObject<string>(res); } catch { val = res; }
                if (string.IsNullOrEmpty(val) == false && val != "pending")
                {
                    return val;
                }
            }
            return "TIMEOUT";
        }

        async void PollTick(object sender, EventArgs e)
        {
            ticks++;
            try
            {
                if (stage == 0)
                {
                    string nav = await EvalJS("document.readyState + '|' + location.href");
                    if (nav != null && nav.Contains("complete"))
                    {
                        stage = 1;
                        ticks = 0;
                        Log("pagina carregada");
                    }
                    else if (ticks > 30)
                    {
                        Log("TIMEOUT aguardando pagina");
                        Finish();
                    }
                }
                else if (stage == 1)
                {
                    // aguardar sincronizacao pos-login terminar
                    string body = await EvalJS("(document.body?document.body.innerText:'').substring(0,200).replace(/\\s+/g,' ')");
                    bool carregando = body != null && (body.Contains("Carregando suas conversas") || body.Contains("Não feche esta janela") || body.Contains("Nao feche esta janela"));
                    bool qr = body != null && string.IsNullOrEmpty(body);
                    if (ticks % 5 == 1)
                    {
                        Log("estado pagina: " + (body == null ? "null" : body.Substring(0, Math.Min(120, body.Length))));
                    }
                    if (carregando)
                    {
                        if (ticks > 120)
                        {
                            Log("sincronizacao nao terminou em 4 min - seguindo mesmo assim");
                            stage = 2;
                            ticks = 0;
                        }
                        // continua esperando
                    }
                    else
                    {
                        Log("sincronizacao concluida (ou ja estava pronta)");
                        stage = 2;
                        ticks = 0;
                    }
                }
                else if (stage == 2)
                {
                    // injetar / confirmar WPP (com tolerancia a recarga)
                    string hasWpp = await EvalJS("typeof window.WPP !== 'undefined' ? 'sim' : 'nao'");
                    if (hasWpp == "sim")
                    {
                        string ver = await EvalJS("WPP.version || 'sem-versao'");
                        Log("WPP presente, versao: " + ver);
                        stage = 3;
                        ticks = 0;
                    }
                    else
                    {
                        if (reinjections == 0 || ticks % 4 == 0)
                        {
                            if (reinjections > 8)
                            {
                                Log("WPP nao aparece apos multiplas reinjecoes");
                                Finish();
                                return;
                            }
                            reinjections++;
                            Log("WPP ausente - injetando (tentativa " + reinjections + ")");
                            await wv.ExecuteScriptAsync(csv);
                            await System.Threading.Tasks.Task.Delay(5000);
                        }
                    }
                }
                else if (stage == 3)
                {
                    // confirmar que a pagina nao recarregou de novo e disparar isReady
                    string hasWpp = await EvalJS("typeof window.WPP !== 'undefined' ? 'sim' : 'nao'");
                    if (hasWpp != "sim")
                    {
                        Log("WPP sumiu (recarga da pagina) - voltando a injetar");
                        stage = 2;
                        ticks = 0;
                        return;
                    }
                    await wv.ExecuteScriptAsync("if(!window.__ready){window.__ready='aguardando';Promise.resolve(WPP.isReady()).then(()=>{window.__ready='sim';}).catch((e)=>{window.__ready='erro:'+e;});}");
                    string ready = await EvalJS("window.__ready || 'pendente'");
                    if (ticks % 5 == 1)
                    {
                        Log("WPP isReady: " + ready);
                    }
                    if (ready == "sim" || ticks > 45)
                    {
                        stage = 4;
                        ticks = 0;
                        Log("indo para diagnostico (ready=" + ready + ")");
                    }
                }
                else if (stage == 4)
                {
                    poll.Stop();
                    Log("--- DIAGNOSTICOS ---");
                    string qr = await EvalJS("(function(){var q=document.querySelector('canvas');return JSON.stringify({canvas:!!q});})()");
                    Log("PAGINA: " + qr);
                    string js = "window.__t='pending';(async()=>{const r={};try{r.version=WPP.version||'?';}catch(e){r.version='ERR';}try{const q=await WPP.contact.queryExists('" + sendJid + "');r.q_existe=q?('wid='+String(q.wid)+'|lid='+String(q.lid||'null')+'|biz='+q.biz):'NULL';}catch(e){r.q_existe='ERRO:'+(e&&e.message?e.message:String(e));}window.__t=JSON.stringify(r);})().catch(e=>{window.__t='OUTER:'+(e&&e.message?e.message:String(e));});";
                    string diag = await RunAsyncJS(js, 20, 1000);
                    Log("DIAG: " + diag);
                    string jsProbe = "window.__t='pending';(async()=>{try{const cs=await WPP.contact.list();const arr=[];for(let i=0;i<cs.length&&arr.length<6;i++){const ct=cs[i];arr.push({id:ct.id?String(ct.id&&(''+ct.id)):null,pn:ct.pn?String(ct.pn):null,name:ct.name||ct.pushname||null});}window.__t=JSON.stringify({total:cs.length,amostra:arr});}catch(e){window.__t='ERRO:'+(e&&e.message||e);}})();";
                    string probe = await RunAsyncJS(jsProbe, 15, 1000);
                    Log("CONTATOS: " + probe);
                    string jsChatProbe = "window.__t='pending';(async()=>{try{const c=await WPP.chat.find('228423574261910@lid');if(!c){window.__t='chat nao encontrado';return;}const o={};try{o.id=String(c.id);}catch(e){}try{o.contactId=c.contact?String(c.contact.id):null;}catch(e){}try{o.contactPn=c.contact&&c.contact.pn?String(c.contact.pn):null;}catch(e){}try{o.pn=c.pn?String(c.pn):null;}catch(e){}try{o.keys=Object.keys(c.toJSON?c.toJSON():c).slice(0,40);}catch(e){}try{o.contactKeys=c.contact?Object.keys(c.contact.toJSON?c.contact.toJSON():c.contact).slice(0,40):null;}catch(e){}window.__t=JSON.stringify(o);}catch(e){window.__t='ERRO:'+(e&&e.message||e);}})();";
                    string chatProbe = await RunAsyncJS(jsChatProbe, 15, 1000);
                    Log("CHAT MODEL (por LID): " + chatProbe);
                    Log("--- ENVIANDO MENSAGEM DE TESTE para " + target + " (fallback LID) ---");
                    string msg = "Teste automatico do disparador " + DateTime.Now.ToString("HH:mm:ss");
                    string jsMsg = Newtonsoft.Json.JsonConvert.SerializeObject(msg);
                    string jsSend = "window.__t='pending';(async()=>{function info(r){let i={};try{i.type=typeof r;if(r&&r.id){i.id=String(r.id);}if(r&&r.ack!==undefined){i.ack=r.ack;}}catch(e){}return JSON.stringify(i);}const ID=" + Newtonsoft.Json.JsonConvert.SerializeObject(sendJid) + ";async function resolveLid(){try{const v=localStorage.getItem('__lidmap_'+String(ID).split('@')[0]);if(v&&String(v).indexOf('@lid')>0)return v;}catch(e5){}if(window.__lidResolve){try{const l=await window.__lidResolve(ID);if(l)return l;}catch(e0){}}try{const q=await WPP.contact.queryExists(ID);if(q&&q.lid)return String(q.lid);if(q&&q.wid&&q.wid.server==='lid')return String(q.wid);if(q&&q.wid&&q.wid.server==='c.us'){try{const pe=await WPP.contact.getPnLidEntry(ID);if(pe&&pe.lid)return pe.lid._serialized;}catch(e3){}}}catch(e){}return null;}try{const r=await WPP.chat.sendTextMessage(ID," + jsMsg + ",{createChat:true});window.__t='OK_PHONE|'+info(r);}catch(e){const em=String(e&&e.message||e);const lid=await resolveLid();if(lid){try{const r2=await WPP.chat.sendTextMessage(lid," + jsMsg + ",{createChat:true});try{localStorage.setItem('__lidmap_'+String(ID).split('@')[0],lid);}catch(e6){}window.__t='OK_LID|'+lid+'|'+info(r2);}catch(e2){window.__t='ERRO_LIDFB|'+lid+'|'+String(e2&&e2.message||e2);}}else{window.__t='ERRO_SEMLID|'+em;}}})();";
                    string sendRes = await RunAsyncJS(jsSend, 40, 1000);
                    Log("ENVIO: " + sendRes);
                    if (sendRes != null && sendRes.StartsWith("ERRO_SEMLID"))
                    {
                        Log("--- resolucao falhou; semeando mapa LID conhecido e reenviando ---");
                        string seedJs = "localStorage.setItem('__lidmap_" + target + "','228423574261910@lid');'semeado'";
                        string seedRes = await EvalJS(seedJs);
                        Log("semente: " + seedRes);
                        sendRes = await RunAsyncJS(jsSend, 40, 1000);
                        Log("ENVIO 2: " + sendRes);
                    }
                    Log("--- verificando entrega (15s) ---");
                    await System.Threading.Tasks.Task.Delay(15000);
                    string jsAck = "window.__t='pending';(async()=>{async function dump(jid){try{const msgs=await WPP.chat.getMessages(jid,{count:3});const arr=[];for(const m of msgs){try{arr.push({body:String(m.body||'').substring(0,40),ack:m.ack,fromMe:(m.self==='out'||m.fromMe)});}catch(e){}}return arr;}catch(e){return 'ERRO:'+(e&&e.message||e);}}const q=await WPP.contact.queryExists('" + sendJid + "').catch(()=>null);const lid=q?((q.lid?String(q.lid):(q.wid&&q.wid.server==='lid'?String(q.wid):null))):null;const res={phone:await dump('" + sendJid + "'),lid:lid?await dump(lid):'sem-lid'};window.__t=JSON.stringify(res);})().catch(e=>{window.__t='OUTER:'+(e&&e.message||e);});";
                    string ackRes = await RunAsyncJS(jsAck, 25, 1000);
                    Log("ULTIMAS MSGS (ack: 0=fila,1=enviado,2=entregue,3=lida): " + ackRes);
                    bool ok = sendRes != null && (sendRes.StartsWith("OK_PHONE") || sendRes.StartsWith("OK_LID"));
                    Log("--- RESULTADO: " + (ok ? "MENSAGEM CRIADA E ENVIADA" : "FALHOU") + " ---");
                    Finish();
                }
            }
            catch (Exception ex)
            {
                Log("ERRO estagio " + stage + ": " + ex.Message);
                if (stage == 4) { Finish(); }
            }
        }

        void Finish()
        {
            try
            {
                Log("=== fim do teste ===");
                File.AppendAllText(logPath, log.ToString(), Encoding.UTF8);
            }
            catch { }
            this.BeginInvoke(new Action(() => { Application.Exit(); }));
        }
    }
}
