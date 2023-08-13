class HTMLElement : Element, HTMLOrSVGElement {
  public HTMLElement(Document document, string? ns, string name) : base(document, ns, name) {}
  public string title { get; set; }
  public string lang { get; set; }

  public bool? hidden { get; set; }
  public string inner_text { get; set; }
  public string outer_text { get; set; }
  public long tab_index { get; set; }
  public bool autofocus { get; set; }
}

class HTMLHeadElement : HTMLElement {
  public HTMLHeadElement(Document document, string? ns, string name) : base(document, ns, name) {}
}
