class Text : CharacterData {
  public Text(Document document, string data = "") : base(document, data) { }
  public Text split_text(ulong offset) {
    return new Text(node_document);
  }
  public string whole_text { get { return ""; } }
  public override string ToString() {
    return $"[Text: '{data}']";
  }
}

