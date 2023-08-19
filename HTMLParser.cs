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

  private bool _frameset_ok = false;
  InsertionMode _insertion_mode = InsertionMode.Initial;
  Document _document = new();
  // Initially, the stack of open elements is empty. The stack grows downwards;
  //   the topmost node on the stack is the first one added to the stack,
  //   and the bottommost node of the stack is the most recently added node in the stack
  //   (notwithstanding when the stack is manipulated in a random access fashion as part of the handling for misnested tags).
  Stack<Element> _open_elements = new();
  Element? _head_element = null;
  Element? _form_element = null;
  InsertionMode _original_insertion_mode;
  int _script_nesting_level = 0;
  bool _parser_pause_flag = false;
  private HTMLParser? _active_speculative_html_parser = null;
  private bool _forster_parenting_enabled = false;

  Node current_node { get { return _open_elements.Peek(); } }
  void pop_current_node() {
    _open_elements.Pop();
  }
  

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

  void insert_a_comment(HTMLToken token) {
    throw new NotImplementedException();
  }

  // https://html.spec.whatwg.org/#appropriate-place-for-inserting-a-node
  Node find_appropriate_place_for_inserting_a_node() {
    // 1. If there was an override target specified, then let target be the override target.
    //    Otherwise, let target be the current node.
    // FIXME: handle override target
    var target = current_node;
    // 2. Determine the adjusted insertion location using the first matching steps from the following list:
    // If foster parenting is enabled and target is a table, tbody, tfoot, thead, or tr element
    if (_forster_parenting_enabled && target.is_element_of("table", "tbody", "tfoot" ,"thead", "tr")) {
      // 1. Let last template be the last template element in the stack of open elements, if any.
      // TODO

      // 2. Let last table be the last table element in the stack of open elements, if any.
      var last_table = _open_elements.LastOrDefault(e => e.is_element_of("table"));

      // 3. If there is a last template and either there is no last table, or there is one, but last template is lower (more recently added) than last table in the stack of open elements, then: let adjusted insertion location be inside last template's template contents, after its last child (if any), and abort these steps.

      // 4. If there is no last table, then let adjusted insertion location be inside the first element in the stack of open elements (the html element), after its last child (if any), and abort these steps. (fragment case)

      // 5. If last table has a parent node, then let adjusted insertion location be inside last table's parent node, immediately before last table, and abort these steps.
      if (last_table?.parent_node != null) {
        throw new NotImplementedException();
      }

      // 6. Let previous element be the element immediately above last table in the stack of open elements.
      var previous_element = _open_elements.SkipWhile(e => e != last_table).Skip(1).FirstOrDefault();

      // 7. Let adjusted insertion location be inside previous element, after its last child (if any).
      if (previous_element != null) return previous_element;
    }

    // Otherwise
    //    Let adjusted insertion location be inside target, after its last child (if any).
    var adjusted_insertion_location = target;

    // 3. If the adjusted insertion location is inside a template element,
    //    let it instead be inside the template element's template contents, after its last child (if any).
    // FIXME

    // 4. Return the adjusted insertion location.
    return adjusted_insertion_location;
  }

  // https://html.spec.whatwg.org/#insert-a-foreign-element
  Element insert_a_foreign_element(HTMLToken token, string ns) {
    // 1. Let the adjusted insertion location be the appropriate place for inserting a node.
    var adjusted_insertion_location = find_appropriate_place_for_inserting_a_node();
    // 2. Let element be the result of creating an element for the token in the given namespace,
    //    with the intended parent being the element in which the adjusted insertion location finds itself.
    var element = create_element_for_token(token, adjusted_insertion_location, ns);
    // 3. If it is possible to insert element at the adjusted insertion location, then:
    // FIXME: I don't understand what is "possible to insert element at"
    if (is_possible_to_insert_element_at(adjusted_insertion_location)) {
      adjusted_insertion_location.append_child(element);
    }
    _open_elements.Push(element);
    return element;
  }

  bool is_possible_to_insert_element_at(Node location) {
    return true;
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
  void run_before_html_mode(HTMLToken token) {
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

  // https://html.spec.whatwg.org/#the-before-head-insertion-mode
  void run_before_head_mode(HTMLToken token) {
    if (token.is_space_character) {
      // Ignore the token.
      return;
    }
    if (token.is_comment) {
      // Insert a comment
      insert_a_comment(token);
      return;
    }
    if (token.is_doctype) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }
    if (token.is_start_tag_of("html")) {
      throw new NotImplementedException();
    }
    if (token.is_start_tag_of("head")) {
      // Insert an HTML element for the token.
      // https://html.spec.whatwg.org/#insert-an-html-element
      var element = insert_a_foreign_element(token, Namespaces.HTML);
      // Set the head element pointer to the newly created head element.
      _head_element = element;
      // Switch the insertion mode to "in head".
      _insertion_mode = InsertionMode.InHead;
      return;
    }
    if (token.is_end_tag_of("head", "body", "html", "br")) {
      // Act as described in the "anything else" entry below.
    } else if (token.is_end_tag) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }
    // Insert an HTML element for a "head" start tag token with no attributes.
    var ele = insert_a_foreign_element(token, Namespaces.HTML);

    // Set the head element pointer to the newly created head element.
    _head_element = ele;

    // Switch the insertion mode to "in head".
    _insertion_mode = InsertionMode.InHead;

    // Reprocess the current token.
    reprocess_token();
  }

  // https://html.spec.whatwg.org/#parsing-elements-that-contain-only-text
  void parse_element_contain_only_text(HTMLToken token, bool is_raw_text) {
    Console.WriteLine($"parse_element_contain_only_text: token={token}, is_raw_text={is_raw_text}");
    // The generic raw text element parsing algorithm and the generic RCDATA element parsing algorithm consist of the following steps. These algorithms are always invoked in response to a start tag token.

    // 1. Insert an HTML element for the token.
    insert_a_foreign_element(token, Namespaces.HTML);

    // 2. If the algorithm that was invoked is the generic raw text element parsing algorithm, switch the tokenizer to the RAWTEXT state; otherwise the algorithm invoked was the generic RCDATA element parsing algorithm, switch the tokenizer to the RCDATA state.
    if (is_raw_text) {
      _tokenizer.switch_to_raw_text_state();
    } else {
      _tokenizer.switch_to_rcdata_state();
    }

    // 3. Let the original insertion mode be the current insertion mode.
    _original_insertion_mode = _insertion_mode;

    // 4. Then, switch the insertion mode to "text".
    _insertion_mode = InsertionMode.Text;
    Console.WriteLine("switch to text mode");
  }

  // https://html.spec.whatwg.org/#parsing-main-inhead
  void run_in_head_mode(HTMLToken token) {
    Console.WriteLine($"run_in_head_mode(HTMLToken token) {token}");
    if (token.is_space_character) {
      insert_a_character(token.comment_or_character.data);
      return;
    }
    if (token.is_comment) {
      insert_a_comment(token);
      return;
    }
    if (token.is_doctype) {
      on_error("parse error");
      return;
    }

    if (token.is_start_tag_of("html")) {
      // Process the token using the rules for the "in body" insertion mode.
      run_in_body_mode(token);
    }

    // A start tag whose tag name is one of: "base", "basefont", "bgsound", "link"
    if (token.is_start_tag_of("base", "basefont", "bgsound", "link")) {
      // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
      insert_a_foreign_element(token, Namespaces.HTML);
      pop_current_node();
      // Acknowledge the token's self-closing flag, if it is set.
      if (token.is_self_closing) {
        acknowledge_self_closing_flag();
      }
      return;
    }
    // A start tag whose tag name is "meta"
    if (token.is_start_tag_of("meta")) {
      // Insert an HTML element for the token. Immediately pop the current node off the stack of open elements.
      insert_a_foreign_element(token, Namespaces.HTML);
      pop_current_node();
      // Acknowledge the token's self-closing flag, if it is set.
      if (token.is_self_closing) {
        acknowledge_self_closing_flag();
      }

      // FIXME:
      // If the active speculative HTML parser is null, then:

          // 1. If the element has a charset attribute, and getting an encoding from its value results in an encoding, and the confidence is currently tentative, then change the encoding to the resulting encoding.

          // 2. Otherwise, if the element has an http-equiv attribute whose value is an ASCII case-insensitive match for the string "Content-Type", and the element has a content attribute, and applying the algorithm for extracting a character encoding from a meta element to that attribute's value returns an encoding, and the confidence is currently tentative, then change the encoding to the extracted encoding.
      return;
    }

    // A start tag whose tag name is "title"
    if (token.is_start_tag_of("title")) {
      // Follow the generic RCDATA element parsing algorithm.
      // https://html.spec.whatwg.org/#generic-rcdata-element-parsing-algorithm
      parse_element_contain_only_text(token, is_raw_text:false);
      return;
    }

    // A start tag whose tag name is "noscript", if the scripting flag is enabled
    // A start tag whose tag name is one of: "noframes", "style"
    if (token.is_start_tag_of("noscript", "noframes", "style")) {
      // Follow the generic raw text element parsing algorithm.
      // https://html.spec.whatwg.org/#generic-raw-text-element-parsing-algorithm
      parse_element_contain_only_text(token, is_raw_text:true);
      return;
    }

    // A start tag whose tag name is "noscript", if the scripting flag is disabled
    if (token.is_start_tag_of("noscript")) {
      // Insert an HTML element for the token.
      insert_a_foreign_element(token, Namespaces.HTML);
      // Switch the insertion mode to "in head noscript".
      _insertion_mode = InsertionMode.InHeadNoscript;
      return;
    }

    // A start tag whose tag name is "script"
    if (token.is_start_tag_of("script")) {
      // 1. Let the adjusted insertion location be the appropriate place for inserting a node.
      var adjusted_insertion_location = find_appropriate_place_for_inserting_a_node();;
      // 2. Create an element for the token in the HTML namespace, with the intended parent being the element in which the adjusted insertion location finds itself.
      var element = create_element_for_token(token, adjusted_insertion_location, Namespaces.HTML);

      // 3. Set the element's parser document to the Document, and set the element's force async to false.
      // TODO

      // 4. If the parser was created as part of the HTML fragment parsing algorithm, then set the script element's already started to true. (fragment case)
      // TODO

      // 5. If the parser was invoked via the document.write() or document.writeln() methods, then optionally set the script element's already started to true. (For example, the user agent might use this clause to prevent execution of cross-origin scripts inserted via document.write() under slow network conditions, or when the page has already taken a long time to load.)
      // TODO

      // 6. Insert the newly created element at the adjusted insertion location.
      adjusted_insertion_location.append_child(element);

      // 7. Push the element onto the stack of open elements so that it is the new current node.
      _open_elements.Push(element);

      // 8. Switch the tokenizer to the script data state.
      _tokenizer.switch_to_script_data_state();

      // 9. Let the original insertion mode be the current insertion mode.
      _original_insertion_mode = _insertion_mode;

      // 10. Switch the insertion mode to "text".
      _insertion_mode = InsertionMode.Text;
      return;
    }

    // An end tag whose tag name is "head"
    if (token.is_end_tag_of("head")) {
      // Pop the current node (which will be the head element) off the stack of open elements.
      pop_current_node();
      // Switch the insertion mode to "after head".
      _insertion_mode = InsertionMode.AfterHead;
      return;
    }

    // An end tag whose tag name is one of: "body", "html", "br"
    if (token.is_end_tag_of("body", "html", "br")) {
      // Act as described in the "anything else" entry below.
    }

    // A start tag whose tag name is "template"
    if (token.is_start_tag_of("template")) {
      // Insert an HTML element for the token.
      // Insert a marker at the end of the list of active formatting elements.
      // Set the frameset-ok flag to "not ok".
      // Switch the insertion mode to "in template".
      // Push "in template" onto the stack of template insertion modes so that it is the new current template insertion mode.
      throw new NotImplementedException();
    }

    // An end tag whose tag name is "template"
    if (token.is_end_tag_of("template")) {
      throw new NotImplementedException();
    }

    // A start tag whose tag name is "head"
    // Any other end tag
    if (token.is_start_tag_of("head") || token.is_end_tag) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }

    // Anything else
    // Pop the current node (which will be the head element) off the stack of open elements.
    pop_current_node();
    // Switch the insertion mode to "after head".
    _insertion_mode = InsertionMode.AfterHead;
    // Reprocess the token.
    reprocess_token();
  }

  // https://html.spec.whatwg.org/#acknowledge-self-closing-flag
  // When a start tag token is emitted with its self-closing flag set, if the flag is not acknowledged when it is processed by the tree construction stage, that is a non-void-html-element-start-tag-with-trailing-solidus parse error.
  private void acknowledge_self_closing_flag() {
    // FIXME: what is this?
  }

  // https://html.spec.whatwg.org/#insert-a-character
  private void insert_a_character(string character) {
    // 1. Let data be the characters passed to the algorithm, or, if no characters were explicitly specified, the character of the character token being processed.
    var data = character;
    // 2. Let the adjusted insertion location be the appropriate place for inserting a node.
    var adjusted_insertion_location = find_appropriate_place_for_inserting_a_node();
    // 3. If the adjusted insertion location is in a Document node, then return.
    if (adjusted_insertion_location is Document) return;

    // 4. If there is a Text node immediately before the adjusted insertion location, then append data to that Text node's data. 
    //    Otherwise, create a new Text node whose data is data and whose node document is the same as that of the element in which the adjusted insertion location finds itself, and insert the newly created node at the adjusted insertion location.
    if (adjusted_insertion_location.last_child is Text txt) {
      txt.append_data(data);
      return;
    } 
    var text = new Text(adjusted_insertion_location.node_document, data);
    adjusted_insertion_location.append_child(text);
  }

  void run_in_head_noscript_mode(HTMLToken token) {
    throw new NotImplementedException();
  }

  // https://html.spec.whatwg.org/#the-after-head-insertion-mode
  void run_after_head_mode(HTMLToken token) {
    if (token.is_space_character) {
      insert_a_character(token.comment_or_character.data);
      return;
    }
    if (token.is_comment) {
      insert_a_comment(token);
      return;
    }
    if (token.is_doctype) {
      on_error("parse error");
      return;
    }
    if (token.is_start_tag_of("html")) {
      run_in_body_mode(token);
      return;
    }
    if (token.is_start_tag_of("body")) {
      // Insert an HTML element for the token.
      insert_a_foreign_element(token, Namespaces.HTML);
      // Set the frameset-ok flag to "not ok".
      _frameset_ok = false;
      // Switch the insertion mode to "in body".
      _insertion_mode = InsertionMode.InBody;
      return;
    }
    if (token.is_start_tag_of("frameset")) {
      // Insert an HTML element for the token.
      insert_a_foreign_element(token, Namespaces.HTML);
      // Switch the insertion mode to "in frameset".
      _insertion_mode = InsertionMode.InFrameset;
      return;
    }
    if (token.is_start_tag_of("base", "basefont", "bgsound", "link", "meta", "noframes", "script", "style", "template", "title")) {
      throw new NotImplementedException();
    }
    if (token.is_end_tag_of("template")) {
      throw new NotImplementedException();
    }
    if (token.is_end_tag_of("body", "html", "br")) {
      // Act as described in the "anything else" entry below.
    }
    if (token.is_start_tag_of("head") || token.is_end_tag) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }

    // Insert an HTML element for a "body" start tag token with no attributes.
    insert_a_foreign_element(new HTMLToken(HTMLToken.Type.StartTag, "body"), Namespaces.HTML);
    // Switch the insertion mode to "in body".
    _insertion_mode = InsertionMode.InBody;
    // Reprocess the current token.
    reprocess_token();
  }
  void run_in_body_mode(HTMLToken token) {
    if (token.is_null_character) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }
    if (token.is_space_character) {
      // Reconstruct the active formatting elements, if any.
      reconstruct_active_formatting_elements();

      // Insert the token's character.
      insert_a_character(token.comment_or_character.data);
      return;
    }

    if (token.is_character) {
      // Reconstruct the active formatting elements, if any.
      reconstruct_active_formatting_elements();

      // Insert the token's character.
      insert_a_character(token.comment_or_character.data);

      // Set the frameset-ok flag to "not ok".
      _frameset_ok = false;
      return;
    }

    if (token.is_comment) {
      // Insert a comment.
      insert_a_comment(token);
      return;
    }

    if (token.is_doctype) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }

    if (token.is_start_tag_of("html")) {
      // Parse error. Ignore the token.
      on_error("parse error");
      return;
    }

    // A start tag whose tag name is one of: "base", "basefont", "bgsound", "link", "meta", "noframes", "script", "style", "template", "title"
    // An end tag whose tag name is "template"
    if (token.is_start_tag_of("base", "basefont", "bgsound", "link", "meta", "noframes", "script", "style", "template", "title") || token.is_end_tag_of("template")) {
      // Process the token using the rules for the "in head" insertion mode.
      run_in_head_mode(token);
      return;
    }

    if (token.is_start_tag_of("body")) {
      throw new NotImplementedException();
    }

    if (token.is_start_tag_of("frameset")) {
      throw new NotImplementedException();
    }

    if (token.is_eof) {
      // If the stack of template insertion modes is not empty, then process the token using the rules for the "in template" insertion mode.
      // Otherwise, follow these steps:
      // 1. If there is a node in the stack of open elements that is not either a dd element, a dt element, an li element, an optgroup element, an option element, a p element, an rb element, an rp element, an rt element, an rtc element, a tbody element, a td element, a tfoot element, a th element, a thead element, a tr element, the body element, or the html element, then this is a parse error.
      // 2. Stop parsing.
      stop_parsing();
      return;
    }
  }

  // https://html.spec.whatwg.org/#stop-parsing
  private void stop_parsing() {
    throw new NotImplementedException();
  }

  private void reconstruct_active_formatting_elements() {
    throw new NotImplementedException();
  }

  void run_text_mode(HTMLToken token) {
    if (token.is_character) {
      // Insert the token's character.
      insert_a_character(token.comment_or_character.data);
      return;
    }
    if (token.is_eof) {
      // Parse error. Switch the insertion mode to the original insertion mode and reprocess the token.
      on_error("parse error");
      throw new NotImplementedException();
    }

    if (token.is_end_tag_of("script")) {
      // If the active speculative HTML parser is null and the JavaScript execution context stack is empty, then perform a microtask checkpoint.
      // TODO

      // Let script be the current node (which will be a script element).
      var script = current_node;
      // Pop the current node off the stack of open elements.
      pop_current_node();
      // Switch the insertion mode to the original insertion mode.
      _insertion_mode = _original_insertion_mode;

      // Let the old insertion point have the same value as the current insertion point. Let the insertion point be just before the next input character.
      // TODO: document.write

      // Increment the parser's script nesting level by one.
      _script_nesting_level++;

      // If the active speculative HTML parser is null, then prepare the script element script. This might cause some script to execute, which might cause new characters to be inserted into the tokenizer, and might cause the tokenizer to output more tokens, resulting in a reentrant invocation of the parser.
      if (_active_speculative_html_parser == null) {
        Debug.Assert(script is HTMLScriptElement);
        prepare_script_element((HTMLScriptElement)script);
      }
      // TODO

      // Decrement the parser's script nesting level by one. If the parser's script nesting level is zero, then set the parser pause flag to false.
      _script_nesting_level--;
      if (_script_nesting_level == 0) {
        _parser_pause_flag = false;
      }

      // Let the insertion point have the value of the old insertion point. (In other words, restore the insertion point to its previous value. This value might be the "undefined" value.)
      // At this stage, if the pending parsing-blocking script is not null, then:
      //    If the script nesting level is not zero:
      //    Otherwise
      return;
    }

    if (token.is_end_tag) {
      pop_current_node();
      _insertion_mode = _original_insertion_mode;
      return;
    }
  }

  // https://html.spec.whatwg.org/#prepare-the-script-element
  private void prepare_script_element(HTMLScriptElement el) {
    // 1. If el's already started is true, then return.
    if (el.already_started) {
      return;
    }
    // 2. Let parser document be el's parser document.
    var parser_document = el.parser_document; 
    // 3. Set el's parser document to null.
    el.parser_document = null;
    // 4. If parser document is non-null and el does not have an async attribute, then set el's force async to true.
    if (parser_document != null && !el.has_attribute("async")) {
      el.force_async = true;
    }
    // 5. Let source text be el's child text content.
    var source_text = el.child_text_content();
    // 6. If el has no src attribute, and source text is the empty string, then return.
    if (!el.has_attribute("src") && source_text == "") {
      return;
    }
    // 7. If el is not connected, then return.
    if (!el.is_connected) return;

    // 8. If any of the following are true:
    //      el has a type attribute whose value is the empty string;
    //      el has no type attribute but it has a language attribute and that attribute's value is the empty string; or
    //      el has neither a type attribute nor a language attribute
    // then let the script block's type string for this script element be "text/javascript".
    // Otherwise, if el has a type attribute, then let the script block's type string be the value of that attribute with leading and trailing ASCII whitespace stripped.
    if (el.has_attribute("type") && el.get_attribute("type") == ""
    || !el.has_attribute("type") && el.has_attribute("language") && el.get_attribute("language") == ""
    || !el.has_attribute("type") && !el.has_attribute("language")) {
      el.type = "text/javascript";
    } else if (el.has_attribute("type")) {
      el.type = el.get_attribute("type").Trim();
    }
  }

  void run_in_table_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_table_text_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_caption_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_column_group_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_table_body_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_row_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_cell_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_select_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_select_in_table_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_template_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_after_body_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_in_frameset_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_after_frameset_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_after_after_body_mode(HTMLToken token) {
    throw new NotImplementedException();
  }
  void run_after_after_frameset_mode(HTMLToken token) {
    throw new NotImplementedException();
  }

  public Document run() {
    while (true) {
      if (_reprocess_token) {
        Debug.Assert(_next_token != null);
        _reprocess_token = false;
      } else _next_token = _tokenizer.next_token();

      if (_next_token.is_eof) return _document;

      Console.WriteLine($"insertion_mode: {_insertion_mode}; next_token: {_next_token}");

      switch (_insertion_mode) {
        case InsertionMode.Initial:
          run_initial_mode(_next_token);
          break;
        case InsertionMode.BeforeHtml:
          run_before_html_mode(_next_token);
          break;
        case InsertionMode.BeforeHead:
          run_before_head_mode(_next_token);
          break;
        case InsertionMode.InHead:
          run_in_head_mode(_next_token);
          break;
        case InsertionMode.InHeadNoscript:
          run_in_head_noscript_mode(_next_token);
          break;
        case InsertionMode.AfterHead:
          run_after_head_mode(_next_token);
          break;
        case InsertionMode.InBody:
          run_in_body_mode(_next_token);
          break;
        case InsertionMode.Text:
          run_text_mode(_next_token);
          break;
        case InsertionMode.InTable:
          run_in_table_mode(_next_token);
          break;
        case InsertionMode.InTableText:
          run_in_table_text_mode(_next_token);
          break;
        case InsertionMode.InCaption:
          run_in_caption_mode(_next_token);
          break;
        case InsertionMode.InColumnGroup:
          run_in_column_group_mode(_next_token);
          break;
        case InsertionMode.InTableBody:
          run_in_table_body_mode(_next_token);
          break;
        case InsertionMode.InRow:
          run_in_row_mode(_next_token);
          break;
        case InsertionMode.InCell:
          run_in_cell_mode(_next_token);
          break;
        case InsertionMode.InSelect:
          run_in_select_mode(_next_token);
          break;
        case InsertionMode.InSelectInTable:
          run_in_select_in_table_mode(_next_token);
          break;
        case InsertionMode.InTemplate:
          run_in_template_mode(_next_token);
          break;
        case InsertionMode.AfterBody:
          run_after_body_mode(_next_token);
          break;
        case InsertionMode.InFrameset:
          run_in_frameset_mode(_next_token);
          break;
        case InsertionMode.AfterFrameset:
          run_after_frameset_mode(_next_token);
          break;
        case InsertionMode.AfterAfterBody:
          run_after_after_body_mode(_next_token);
          break;
        case InsertionMode.AfterAfterFrameset:
          run_after_after_frameset_mode(_next_token);
          break;
        default:
        break;
      }
    }
  }
}
