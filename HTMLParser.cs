class HTMLParser {
  public HTMLParser(string input) {
    _tokenizer = new HTMLTokenizer(input);
  }

  HTMLTokenizer _tokenizer;
  HTMLToken _next_token = null!;
  bool _reprocess_token = false;
  void reprocess_token() {
    _reprocess_token = true;
  }

  void on_error(string message) {
    Console.WriteLine(message);
  }

  InsertionMode _insertion_mode = InsertionMode.Initial;
  Document _document = new();
  Stack<Element> _open_elements = new();

  enum InsertionMode {
    Initial,
    BeforeHtml,
    BeforeHead,
    InHead,
    InHeadNoscript,
    AfterHead,
    InBody,
    Text,
    InTable,
    InTableText,
    InCaption,
    InColumnGroup,
    InTableBody,
    InRow,
    InCell,
    InSelect,
    InSelectInTable,
    InTemplate,
    AfterBody,
    InFrameset,
    AfterFrameset,
    AfterAfterBody,
    AfterAfterFrameset
  }

  void insert_comment_as_last_child(HTMLToken token) {
    throw new NotImplementedException();
  }

  // https://html.spec.whatwg.org/#the-initial-insertion-mode
  void run_initial_mode() {
    var token = _next_token;
    // A character token that is one of U+0009 CHARACTER TABULATION, U+000A LINE FEED (LF), U+000C FORM FEED (FF), U+000D CARRIAGE RETURN (CR), or U+0020 SPACE
    if (token.is_space_character) {
      // Ignore the token.
      return;
    }

    // A comment token
    if (token.is_comment) {
      // Insert a comment as the last child of the Document object.
      insert_comment_as_last_child(token);
      return;
    }

    // A DOCTYPE token
    if (token.is_doctype) {
      if (token.doctype.name != "html" || token.doctype.public_identifier != null) {
        // Append a DocumentType node to the Document node, with its name set to the name given in the DOCTYPE token, or the empty string if the name was missing; its public ID set to the public identifier given in the DOCTYPE token, or the empty string if the public identifier was missing; and its system ID set to the system identifier given in the DOCTYPE token, or the empty string if the system identifier was missing.
        var doctype = new DocumentType(_document, token.doctype.name ?? "", token.doctype.public_identifier ?? "", token.doctype.system_identifier ?? "");
        _document.append_child(doctype);

        // Then, if the document is not an iframe srcdoc document, and the parser cannot change the mode flag is false, and the DOCTYPE token matches one of the conditions in the following list, then set the Document to quirks mode:
        // FIXME

        // Otherwise, if the document is not an iframe srcdoc document, and the parser cannot change the mode flag is false, and the DOCTYPE token matches one of the conditions in the following list, then then set the Document to limited-quirks mode:
        // FIXME

        // The system identifier and public identifier strings must be compared to the values given in the lists above in an ASCII case-insensitive manner. A system identifier whose value is the empty string is not considered missing for the purposes of the conditions above.
      }

      // Then, switch the insertion mode to "before html".
      _insertion_mode = InsertionMode.BeforeHtml;
      return;
    }

    // If the document is not an iframe srcdoc document, then this is a parse error; if the parser cannot change the mode flag is false, set the Document to quirks mode.
    if (!_document.is_iframe_srcdoc) {
      on_error("parse error");
      if (!_document.parser_cannot_change_the_model) {
        _document.mode = Document.Mode.Quirks;
      }
    }

    // In any case, switch the insertion mode to "before html", then reprocess the token.
    _insertion_mode = InsertionMode.BeforeHtml;
    reprocess_token();
    return;
  }

  // https://html.spec.whatwg.org/#the-before-html-insertion-mode
  void run_before_html_mode() {
    var token = _next_token;
    if (token.is_doctype) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }
    if (token.is_comment) {
      // Insert a comment as the last child of the Document object.
      insert_comment_as_last_child(token);
      return;
    }

    if (token.is_space_character) {
      // Ignore the token.
      return;
    }

    // A start tag whose tag name is "html"
    if (token.is_start_tag_of("html")) {
      // Create an element for the token in the HTML namespace, with the Document as the intended parent.
      var element = create_element_for_token_in_html_namespace(token, _document);
      // Append it to the Document object.
      _document.append_child(element);
      // Put this element in the stack of open elements.
      _open_elements.Push(element);
      // Switch the insertion mode to "before head".
      _insertion_mode = InsertionMode.BeforeHead;
      return;
    }

    // An end tag whose tag name is one of: "head", "body", "html", "br"
    if (token.is_end_tag_of("head", "body", "html", "br")) {
      // Act as described in the "anything else" entry below.
    } else if (token.type == HTMLToken.Type.EndTag) {
      on_error("parse error");
      return;
    }

    // Create an html element whose node document is the Document object. Append it to the Document object. Put this element in the stack of open elements.
    var html = new HTMLHtmlElement(_document);
    _document.append_child(html);
    _open_elements.Push(html);

    // Switch the insertion mode to "before head", then reprocess the token.
    _insertion_mode = InsertionMode.BeforeHead;
    reprocess_token();
  }

  Element create_element_for_token_in_html_namespace(HTMLToken token, Node parent) {
    Console.WriteLine($"create_element_for_token_in_html_namespace: token={token}, parent={parent}");
    return create_element_for_token(token, parent, Namespaces.HTML);
  }

  // https://html.spec.whatwg.org/#create-an-element-for-the-token
  Element create_element_for_token(HTMLToken token, Node parent, string ns) {
    // 1. If the active speculative HTML parser is not null, then return the result of creating a speculative mock element given given namespace, the tag name of the given token, and the attributes of the given token.
    // FIXME

    // 2. Otherwise, optionally create a speculative mock element given given namespace, the tag name of the given token, and the attributes of the given token.
    // FIXME

    // 3. Let document be intended parent's node document.
    var document = parent.node_document;
    // 4. Let local name be the tag name of the token.
    var local_name = token.tag!.name;
    // 5. Let is be the value of the "is" attribute in the given token, if such an attribute exists, or null otherwise.
    var es = token.get_attribute_value("is");

    // 6. Let definition be the result of looking up a custom element definition given document, given namespace, local name, and is.
    // FIXME

    // 7. If definition is non-null and the parser was not created as part of the HTML fragment parsing algorithm, then let will execute script be true. Otherwise, let it be false.
    // FIXME

    // 8. If will execute script is true, then:
    // FIXME

    // 9. Let element be the result of creating an element given document, localName, given namespace, null, and is.
    //    If will execute script is true, set the synchronous custom elements flag; otherwise, leave it unset.
    var element = Document.create_an_element(document, local_name, ns, null, es);
    Console.WriteLine($"create_an_element({document}, {local_name}, {ns}) returns {element}");

    // 10. Append each attribute in the given token to element.
    if (token.tag!.attributes != null) element.append_attribute(token.tag!.attributes);

    // 11 ~ 14
    // FIXME

    // 15. Return element
    return element;
  }

  void run_before_head_mode() {
    Console.WriteLine(_document);
    throw new NotImplementedException();
  }
  void run_in_head_mode() {
    throw new NotImplementedException();
  }
  void run_in_head_noscript_mode() {
    throw new NotImplementedException();
  }
  void run_after_head_mode() {
    throw new NotImplementedException();
  }
  void run_in_body_mode() {
    throw new NotImplementedException();
  }
  void run_text_mode() {
    throw new NotImplementedException();
  }
  void run_in_table_mode() {
    throw new NotImplementedException();
  }
  void run_in_table_text_mode() {
    throw new NotImplementedException();
  }
  void run_in_caption_mode() {
    throw new NotImplementedException();
  }
  void run_in_column_group_mode() {
    throw new NotImplementedException();
  }
  void run_in_table_body_mode() {
    throw new NotImplementedException();
  }
  void run_in_row_mode() {
    throw new NotImplementedException();
  }
  void run_in_cell_mode() {
    throw new NotImplementedException();
  }
  void run_in_select_mode() {
    throw new NotImplementedException();
  }
  void run_in_select_in_table_mode() {
    throw new NotImplementedException();
  }
  void run_in_template_mode() {
    throw new NotImplementedException();
  }
  void run_after_body_mode() {
    throw new NotImplementedException();
  }
  void run_in_frameset_mode() {
    throw new NotImplementedException();
  }
  void run_after_frameset_mode() {
    throw new NotImplementedException();
  }
  void run_after_after_body_mode() {
    throw new NotImplementedException();
  }
  void run_after_after_frameset_mode() {
    throw new NotImplementedException();
  }

  public Document run() {
    while (true) {
      if (_reprocess_token) {
        Debug.Assert(_next_token != null);
        _reprocess_token = false;
      } else _next_token = _tokenizer.next_token();

      if (_next_token.is_eof()) return _document;

      Console.WriteLine($"insertion_mode: {_insertion_mode}; next_token: {_next_token}");

      switch (_insertion_mode) {
        case InsertionMode.Initial:
          run_initial_mode();
          break;
        case InsertionMode.BeforeHtml:
          run_before_html_mode();
          break;
        case InsertionMode.BeforeHead:
          run_before_head_mode();
          break;
        case InsertionMode.InHead:
          run_in_head_mode();
          break;
        case InsertionMode.InHeadNoscript:
          run_in_head_noscript_mode();
          break;
        case InsertionMode.AfterHead:
          run_after_head_mode();
          break;
        case InsertionMode.InBody:
          run_in_body_mode();
          break;
        case InsertionMode.Text:
          run_text_mode();
          break;
        case InsertionMode.InTable:
          run_in_table_mode();
          break;
        case InsertionMode.InTableText:
          run_in_table_text_mode();
          break;
        case InsertionMode.InCaption:
          run_in_caption_mode();
          break;
        case InsertionMode.InColumnGroup:
          run_in_column_group_mode();
          break;
        case InsertionMode.InTableBody:
          run_in_table_body_mode();
          break;
        case InsertionMode.InRow:
          run_in_row_mode();
          break;
        case InsertionMode.InCell:
          run_in_cell_mode();
          break;
        case InsertionMode.InSelect:
          run_in_select_mode();
          break;
        case InsertionMode.InSelectInTable:
          run_in_select_in_table_mode();
          break;
        case InsertionMode.InTemplate:
          run_in_template_mode();
          break;
        case InsertionMode.AfterBody:
          run_after_body_mode();
          break;
        case InsertionMode.InFrameset:
          run_in_frameset_mode();
          break;
        case InsertionMode.AfterFrameset:
          run_after_frameset_mode();
          break;
        case InsertionMode.AfterAfterBody:
          run_after_after_body_mode();
          break;
        case InsertionMode.AfterAfterFrameset:
          run_after_after_frameset_mode();
          break;
        default:
        break;
      }
    }
  }
}
