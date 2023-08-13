class HTMLParser {
  public HTMLParser(string input) {
    _tokenizer = new HTMLTokenizer(input);
  }

  HTMLTokenizer _tokenizer;
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
  void run_initial_mode(HTMLToken token) {
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
        var doctype = new DocumentType(token.doctype.name ?? "", token.doctype.public_identifier ?? "", token.doctype.system_identifier ?? "");
        _document.append_child(doctype);

        // Then, if the document is not an iframe srcdoc document, and the parser cannot change the mode flag is false, and the DOCTYPE token matches one of the conditions in the following list, then set the Document to quirks mode:
        // FIXME

        // Otherwise, if the document is not an iframe srcdoc document, and the parser cannot change the mode flag is false, and the DOCTYPE token matches one of the conditions in the following list, then then set the Document to limited-quirks mode:
        // FIXME

        // The system identifier and public identifier strings must be compared to the values given in the lists above in an ASCII case-insensitive manner. A system identifier whose value is the empty string is not considered missing for the purposes of the conditions above.
      }

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
    _insertion_mode = InsertionMode.BeforeHtml;
    _tokenizer.reprocess_token(token);
    return;
  }

  private void on_error(string message) {
    Console.WriteLine(message);
  }

  void run_before_html_mode(HTMLToken token) {
    if (token.is_doctype) {
      on_error("parse error");
      return;
    }
    if (token.is_comment) {
      insert_comment_as_last_child(token);
      return;
    }

    if (token.is_space_character) {
      return;
    }

    // A start tag whose tag name is "html"
    if (token.type == HTMLToken.Type.StartTag && token.tag!.name == "html") {
      // Create an element for the token in the HTML namespace, with the Document as the intended parent. Append it to the Document object. Put this element in the stack of open elements.
      var element = create_element(token, _document);
      _document.append_child(element);
      _open_elements.Push(element);
      // Switch the insertion mode to "before head".
      _insertion_mode = InsertionMode.BeforeHead;
      return;
    }

    // An end tag whose tag name is one of: "head", "body", "html", "br"
    if (token.type == HTMLToken.Type.EndTag && (new[] { "head", "body", "html", "br" }).Contains(token.tag!.name)) {
      // Act as described in the "anything else" entry below.
    } else if (token.type == HTMLToken.Type.EndTag) {
      on_error("parse error");
      return;
    }

    // Create an html element whose node document is the Document object. Append it to the Document object. Put this element in the stack of open elements.
    var html = _document.create_element("html");
    _document.append_child(html);
    _open_elements.Push(html);

    // Switch the insertion mode to "before head", then reprocess the token.
    _insertion_mode = InsertionMode.BeforeHead;
    _tokenizer.reprocess_token(token);
  }

  private Element create_element(HTMLToken token, Node parent) {
    return _document.create_element(token.tag!.name);
  }

  void run_before_head_mode() {
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
  public void run() {
    while (true) {
      var token = _tokenizer.next_token();
      switch (_insertion_mode) {
        case InsertionMode.Initial:
          run_initial_mode(token);
          break;
        case InsertionMode.BeforeHtml:
          run_before_html_mode(token);
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
