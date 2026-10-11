using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    public partial class InfoWindow : MyMaterialPopOp
    {
        private string readmoreImageBase = "";
        int infoRowNumbr = 0;
        public InfoWindow(int _infoRowNumbr)
        {
            InitializeComponent();
            this.Icon = Strings.AppIcon;
            infoRowNumbr = _infoRowNumbr;
        }

        private void InfoWindow_Load(object sender, EventArgs e)
        {
            initLanguages();
            setText();
        }

        private void setText()
        {
            StringBuilder s = new StringBuilder("");
            s.AppendLine("<html>");
            s.AppendLine("<body style='font-family: monospace; font-size: 13px;'>");
            if (infoRowNumbr == 0)
            {
                s.AppendLine("<h3>[NAME] — Nome do contato</h3>");
                s.AppendLine("<p>É substituído pelo <b>nome do destinatário</b> em cada envio.</p>");
                s.AppendLine("<p><b>De onde vem o nome:</b> a coluna <b>\"Nome\"</b> da sua lista de destinatários (planilha/CSV). Se essa coluna estiver vazia, o sistema tenta buscar o nome do próprio WhatsApp do contato.</p>");
                s.AppendLine("<p><b>Exemplo:</b> \"Oi [NAME], tudo bem?\" vira \"Oi Maria, tudo bem?\"</p>");
                s.AppendLine("<p style='color:red'>Importante: escreva exatamente <b>[NAME]</b> (não use [Nome], {name}, {{Name}} etc.).</p>");
            }
            else if (infoRowNumbr == 1)
            {
                s.AppendLine("<h3>[SPOILER] — Botão \"Ler mais\"</h3>");
                s.AppendLine("<p>Esconde o restante da mensagem atrás de um botão <b>\"Ler mais\"</b> no WhatsApp.</p>");
                s.AppendLine("<p><b>Exemplo:</b> \"Promoção imperdível! [SPOILER] Só hoje: 50% OFF em tudo!\" — o destinatário só vê a primeira parte e clica em \"Ler mais\" para ver o resto.</p>");
                s.AppendLine("<p style='color:red'>Nota: o spoiler pode não funcionar em iPhones (iOS).</p>");
            }
            else if (infoRowNumbr == 2)
            {
                s.AppendLine("<h3>{{sys.date}} — Data e hora do envio</h3>");
                s.AppendLine("<p>Substituído pela <b>data e hora atuais</b> no momento em que cada mensagem é enviada.</p>");
                s.AppendLine("<p><b>Exemplo:</b> \"Promoção válida até {{sys.date}}\" vira \"Promoção válida até 10/10/2026 14:32\".</p>");
            }
            else if (infoRowNumbr == 3)
            {
                s.AppendLine("<h3>{{ RANDOM }} — Número aleatório</h3>");
                s.AppendLine("<p>Substituído por um <b>número aleatório entre 10000 e 50000</b> — diferente em cada envio.</p>");
                s.AppendLine("<p><b>Exemplo:</b> \"Seu cupom de sorte: CUPOM{{ RANDOM }}\" vira \"Seu cupom de sorte: CUPOM28471\".</p>");
            }
            else if (infoRowNumbr == 4)
            {
                s.AppendLine("<h3>{{ KEY :a|b|c }} — Sortear uma palavra</h3>");
                s.AppendLine("<p>O sistema <b>sorteia uma das opções</b> (separadas por |) para cada destinatário. Serve para variar a mensagem e evitar bloqueios.</p>");
                s.AppendLine("<p><b>Exemplo:</b> \"{{ KEY :Bom dia|Boa tarde|Boa noite}}, tudo bem?\" vira \"Bom dia, tudo bem?\" para uns e \"Boa noite, tudo bem?\" para outros.</p>");
                s.AppendLine("<p style='color:red'>Atenção: use exatamente {{ KEY :opção1|opção2 }} — com \" KEY :\" e as opções separadas por |.</p>");
            }
            else if (infoRowNumbr == 5)
            {
                s.AppendLine("<h3>{{NomeDaColuna}} — Valor da coluna da planilha</h3>");
                s.AppendLine("<p>Substituído pelo valor da <b>coluna da sua planilha</b> para aquele destinatário.</p>");
                s.AppendLine("<p><b>Como usar:</b> o nome entre chaves deve ser <b>idêntico ao título da coluna</b> da planilha (maiúsculas/minúsculas não importam).</p>");
                s.AppendLine("<p><b>Exemplo:</b> se sua planilha tem as colunas Nome, Cidade e Produto: \"Oi [NAME], chegou sua encomenda de {{Produto}} em {{Cidade}}!\"</p>");
                s.AppendLine("<p><b>Dica:</b> o nome do contato também funciona como <b>{{name}}</b> — igual a [NAME].</p>");
                s.AppendLine("<p style='color:red'>Atenção: o nome deve ser escrito EXATAMENTE igual ao título da coluna — cuidado com erros de digitação (ex.: coluna \"saudacao\" não é \"suadacao\"). Só funcionam as colunas a partir da 3ª da planilha (a 2ª coluna é o nome, usada com [NAME] ou {{name}}).</p>");
            }
            s.AppendLine("</body></html>");
            webBrowser1.DocumentText = s.ToString();
        }

        private void initLanguages()
        {
            this.Text = Strings.Info;
            materialButton1.Text = Strings.Close;
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
