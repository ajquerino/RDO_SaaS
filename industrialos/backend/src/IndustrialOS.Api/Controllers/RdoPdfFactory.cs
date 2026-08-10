using System.Text.Json;
using IndustrialOS.Application.Pdf;
using IndustrialOS.Application.Storage;
using IndustrialOS.Domain.Entities;
using IndustrialOS.Domain.Services;
using IndustrialOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IndustrialOS.Api.Controllers;

/// <summary>Monta o <see cref="RdoPdfModel"/> a partir do RDO — compartilhado entre a rota
/// autenticada (RdosController) e a rota publica de aprovacao (AprovacaoController).</summary>
public static class RdoPdfFactory
{
    public static async Task<RdoPdfModel> BuildAsync(AppDbContext db, Rdo rdo, IStorage storage, string baseUrl)
    {
        var obra = await db.Obras.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == rdo.ObraId);
        var cliente = obra?.ClienteId is { } cid ? await db.Clientes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cid) : null;
        var resp = rdo.ResponsavelUsuarioId is { } uid ? (await db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == uid))?.Nome : null;
        var itens = await db.ObraItens.IgnoreQueryFilters().Where(i => i.ObraId == rdo.ObraId).ToDictionaryAsync(i => i.Id);

        // Mídias do RDO: fotos embutidas no PDF (bytes) + vídeos como link permanente (abre no app, exige login).
        var midias = await db.RdoMidias.IgnoreQueryFilters().Where(m => m.RdoId == rdo.Id).OrderBy(m => m.CriadoEm).ToListAsync();
        var fotos = midias.Count;
        baseUrl = (baseUrl ?? "").TrimEnd('/');

        var fotosImagens = new List<FotoPdf>();
        foreach (var m in midias.Where(x => x.Tipo != "video").Take(12)) // limita p/ não estourar o PDF
        {
            try
            {
                var bytes = await storage.DownloadAsync(m.R2Key);
                if (bytes.Length > 0) fotosImagens.Add(new FotoPdf(bytes, m.Categoria, m.Descricao));
            }
            catch { /* uma imagem ruim/inacessível NÃO pode quebrar o PDF */ }
        }
        var videos = midias.Where(x => x.Tipo == "video")
            .Select(x => new VideoPdf(x.Descricao, $"{baseUrl}/rdo/{rdo.Id}")).ToList();

        var clima = Doc(rdo.Clima); var jorn = Doc(rdo.Jornada); var seg = Doc(rdo.Seguranca);
        var prox = Doc(rdo.ProximoDia); var plan = Doc(rdo.Planejamento); var dif = Doc(rdo.Dificuldades);

        string? J(string k) => Str(jorn, k);
        string[] condicoes = clima.TryGetProperty("condicoes", out var cc) && cc.ValueKind == JsonValueKind.Array
            ? cc.EnumerateArray().Select(x => x.GetString() ?? "").Where(x => x != "").ToArray() : [];
        int? temp = clima.TryGetProperty("temperatura", out var tp) && tp.ValueKind == JsonValueKind.Number ? tp.GetInt32() : null;

        // Horas / hora extra (calculo no backend)
        HorasModel? horas = null;
        if (JornadaCalculo.Calcular(J("inicio"), J("almoco"), J("retorno"), J("termino"), Bool(jorn, "feriado"), rdo.Data) is { } h)
            horas = new HorasModel(h.DiaTipo, JornadaCalculo.Fmt(h.TrabalhadoMin),
                h.ExtraUtilMin > 0 ? JornadaCalculo.Fmt(h.ExtraUtilMin) : null,
                h.Extra100Min > 0 ? JornadaCalculo.Fmt(h.Extra100Min) : null,
                h.Extra150Min > 0 ? JornadaCalculo.Fmt(h.Extra150Min) : null);

        var pendencias = prox.TryGetProperty("pendencias", out var pe) && pe.ValueKind == JsonValueKind.Array
            ? pe.EnumerateArray().Select(x => new PendenciaRow(Str(x, "descricao"), Str(x, "responsavel"), Str(x, "prazo"), Str(x, "status"))).ToList()
            : [];

        // assinaturas (base64 -> bytes)
        var assDoc = Doc(rdo.Assinaturas);
        AssinaturaModel? Ass(string papel, string label)
        {
            if (assDoc.ValueKind != JsonValueKind.Object || !assDoc.TryGetProperty(papel, out var a) || a.ValueKind != JsonValueKind.Object) return null;
            var nome = Str(a, "nome");
            byte[]? img = null;
            var imgStr = Str(a, "img");
            if (!string.IsNullOrEmpty(imgStr))
            {
                var b64 = imgStr.Contains(',') ? imgStr[(imgStr.IndexOf(',') + 1)..] : imgStr;
                try { img = Convert.FromBase64String(b64); } catch { /* ignora imagem invalida */ }
            }
            return nome is null && img is null ? null : new AssinaturaModel(label, nome, img);
        }
        var assinaturas = new[] { Ass("encarregado", "Encarregado"), Ass("fiscal", "Fiscal"), Ass("supervisor", "Supervisor") }
            .Where(x => x is not null).Select(x => x!).ToList();

        return new RdoPdfModel(
            obra?.Nome ?? "Obra", obra?.Contrato, cliente?.Nome, obra?.Local,
            rdo.Numero, rdo.Revisao, rdo.Data.ToString("dd/MM/yyyy"), rdo.DiaSemana, rdo.Turno, resp, rdo.Status.ToString(),
            condicoes, temp, J("inicio"), J("almoco"), J("retorno"), J("termino"), horas,
            rdo.Efetivo.Select(e => new EfetivoRow(e.Funcao, e.Quantidade, e.HoraExtra)).ToList(),
            rdo.Paralisacoes.Select(p => new ParalisacaoRow(p.Inicio, p.Fim, p.Motivo, p.Descricao)).ToList(),
            rdo.Recursos.Select(r => new RecursoRow(r.Equipamento, r.Quantidade, r.Horas)).ToList(),
            rdo.Servicos.Select(s => new ServicoRow(s.Atividade,
                s.ObraItemId is { } oid && itens.TryGetValue(oid, out var it) ? it.Descricao : null,
                s.Status, s.QtdExec, s.Unidade,
                (int)Math.Round(AvancoCalculo.PctItem(s, s.ObraItemId is { } o && itens.TryGetValue(o, out var it2) ? it2 : null) * 100))).ToList(),
            rdo.Retrabalho.Select(rt => new RetrabalhoRow(rt.Atividade, rt.Pessoas, rt.Horas, rt.Causa, rt.AcaoCorretiva)).ToList(),
            pendencias,
            new SegurancaModel(Bool(seg, "dds"), Bool(seg, "apr"), Bool(seg, "pt"), Bool(seg, "areaIsolada"), Bool(seg, "epis"), Bool(seg, "ferramentas"), Str(seg, "observacoes")),
            new ProximoDiaModel(Str(prox, "maoObra"), Str(prox, "equipamentos"), Str(prox, "materiais"), Str(prox, "ferramentas")),
            new PlanejamentoModel(Str(plan, "servicos"), Str(plan, "prioridades"), Str(plan, "areas")),
            Str(dif, "descricao"), rdo.Ocorrencias, fotos, assinaturas, fotosImagens, videos);
    }

    // ---- helpers ----
    private static JsonElement Doc(string raw) => JsonSerializer.Deserialize<JsonElement>(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
    private static string? Str(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
    private static bool Bool(JsonElement e, string k) => e.ValueKind == JsonValueKind.Object && e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.True;
}
