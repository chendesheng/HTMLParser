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

  public Document() : base(null) {
    node_document = this;
  }

  public bool is_iframe_srcdoc {
    get {
      // FIXME
      return false;
    }
  }

  NodeList get_elements_by_name(string elmentName) {
    throw new NotImplementedException();
  }

  public static Element create_an_element(Document document, string local_name, string ns, string? prefix = null, string? es = null, bool? sync_custom_elements = false) {
    // 1. If prefix was not given, let prefix be null.
    // FIXME

    // 2. If is was not given, let is be null.
    // FIXME

    // 3. Let result be null.
    HTMLElement? result = null;

    // 4. Let definition be the result of looking up a custom element definition given document, namespace, localName, and is.
    // FIXME
    // 5. If definition is non-null, and definition’s name is not equal to its local name (i.e., definition represents a customized built-in element), then:
    // FIXME

    // 6. Otherwise, if definition is non-null, then:
    {
      // 1. If the synchronous custom elements flag is set, then run these steps while catching any exceptions:
      // FIXME

      // 2. Otherwise:
      {
        // 1. Set result to a new element that implements the HTMLElement interface, with no attributes,
        //      namespace set to the HTML namespace,
        //      namespace prefix set to prefix,
        //      local name set to localName,
        //      custom element state set to "undefined",
        //      custom element definition set to null,
        //      is value set to null,
        //      and node document set to document.
        if (local_name == "html") {
          result = new HTMLHtmlElement(document);
        } else {
          result = new HTMLElement(document, Namespaces.HTML, local_name);
        }
      }
    }

    return result;
  }

  public Element create_element(string name) {
    throw new NotImplementedException();
  }

  public Element create_element(string name, DOMString options) {
    throw new NotImplementedException();
  }
  public Element create_element(string name, Dictionary<DOMString, DOMString> options) {
    throw new NotImplementedException();
  }
}
