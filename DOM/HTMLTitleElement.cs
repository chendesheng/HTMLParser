class HTMLTitleElement : HTMLElement {
  public HTMLTitleElement(Document document, string? ns = Namespaces.HTML) : base(document, ns, "title") {
  }

  public DOMString text { get { return child_text_content(); } set{ string_replace_all(value); } }

}