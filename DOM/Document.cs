class Document : Node {
  string title { get; set; }
  HTMLElement? body { get; set; }
  HTMLHeadElement? head { get; set; }

  public bool parser_cannot_change_the_model = false;
  public enum Mode {
    NoQuirks,
    Quirks,
    LimitedQuirks,
  }
  public Mode mode = Mode.NoQuirks;

  public bool is_iframe_srcdoc {
    get {
      // FIXME
      return false;
    }
  }

  NodeList get_elements_by_name(string elmentName) {
    throw new NotImplementedException();
  }

  internal Element create_element(string name) {
    throw new NotImplementedException();
  }
}
