using System.Xml.XPath;

class HTMLScriptElement : HTMLElement {
  public HTMLScriptElement(Document document, string? ns = Namespaces.HTML) : base(document, ns, "script") {
  }
  public bool already_started { get; set; } = false;
  public string? type { get; set; } = "";
  public string? src { get; set; } = "";
  public Document? parser_document { get; set; } = null;
  public Document? preparation_time_document { get; set; } = null;
  public bool force_async { get; set; } = true;
}