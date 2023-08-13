global using System.Text;
global using System.Diagnostics;

class HTMLTokenizer {
  public HTMLTokenizer(string input) {
    _input = input;
  }

  public HTMLToken next_token() {
    while (true) {
      if (_emitting_tokens.Count > 0) {
        var token = _emitting_tokens[0];
        _emitting_tokens.RemoveAt(0);
        _emitted_tokens.Add(token);
        return token;
      }

      switch (_state) {
        case State.Data:
          consume_next_input_character();
          if (_current_input_character == '&') {
            _return_state = State.Data;
            switch_to(State.CharacterReference);
          } else if (_current_input_character == '<') {
            switch_to(State.TagOpen);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            emit_current_input_character();
          } else if (_current_input_character == EOF) {
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.RCDATA:
          consume_next_input_character();
          if (_current_input_character == '&') {
            _return_state = State.RCDATA;
            switch_to(State.CharacterReference);
          } else if (_current_input_character == '<') {
            switch_to(State.RCDATALessThanSign);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.RAWTEXT:
          consume_next_input_character();
          if (_current_input_character == '<') {
            switch_to(State.RAWTEXTLessThanSign);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.ScriptData:
          consume_next_input_character();
          if (_current_input_character == '<') {
            switch_to(State.ScriptDataLessThanSign);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.PLAINTEXT:
          consume_next_input_character();
          if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.TagOpen:
          consume_next_input_character();
          if (_current_input_character == '!') {
            switch_to(State.MarkupDeclarationOpen);
          } else if (_current_input_character == '/') {
            switch_to(State.EndTagOpen);
          } else if (is_ascii_alpha(_current_input_character)) {
            _current_token = new HTMLToken(HTMLToken.Type.StartTag);
            reconsume_in_state(State.TagName);
          } else if (_current_input_character == '?') {
            parse_error("unexpected-question-mark-instead-of-tag-name");
            emit_character_token('<');
            emit_end_of_file_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-before-tag-name");
            emit_character_token('<');
            emit_end_of_file_token();
          } else {
            parse_error("invalid-first-character-of-tag-name");
            emit_character_token('<');
            reconsume_in_state(State.Data);
          }
          break;
        case State.EndTagOpen:
          consume_next_input_character();
          if (is_ascii_alpha(_current_input_character)) {
            _current_token = new HTMLToken(HTMLToken.Type.EndTag);
            reconsume_in_state(State.TagName);
          } else if (_current_input_character == '>') {
            parse_error("missing-end-tag-name");
            switch_to(State.Data);
          } else if (_current_input_character == EOF) {
            parse_error("eof-before-tag-name");
            emit_character_token('<');
            emit_character_token('/');
            emit_end_of_file_token();
          } else {
            parse_error("invalid-first-character-of-tag-name");
            _current_token = new HTMLToken(HTMLToken.Type.Comment);
            reconsume_in_state(State.BogusComment);
          }
          break;
        case State.TagName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.BeforeAttributeName);
          } else if (_current_input_character == '/') {
            switch_to(State.SelfClosingStartTag);
          } else if (_current_input_character == '>') {
            Debug.Assert(_current_token?.tag != null);
            emit_current_token();
            switch_to(State.Data);
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(to_lower(current_input_character));
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-tag");
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(current_input_character);
          }
          break;
        case State.RCDATALessThanSign:
          consume_next_input_character();
          if (_current_input_character == '/') {
            _temporary_buffer = new StringBuilder();
            switch_to(State.RCDATAEndTagOpen);
          } else {
            emit_character_token('<');
            reconsume_in_state(State.RCDATA);
          }
          break;
        case State.RCDATAEndTagOpen:
          consume_next_input_character();
          if (is_ascii_alpha(_current_input_character)) {
            _current_token = new HTMLToken(HTMLToken.Type.EndTag);
            reconsume_in_state(State.RCDATAEndTagName);
          } else {
            emit_character_token('<');
            emit_character_token('/');
            reconsume_in_state(State.RCDATA);
          }
          break;
        case State.RCDATAEndTagName:
          if (is_white_space(_current_input_character)) {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.BeforeAttributeName);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '/') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.SelfClosingStartTag);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '>') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.Data);
              emit_current_token();
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(to_lower(current_input_character));
            continue;
          } else if (is_ascii_lower_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(current_input_character);
            append_to_temp_buffer(current_input_character);
            continue;
          }

          emit_character_token('<');
          emit_character_token('/');
          Debug.Assert(_temporary_buffer != null);
          foreach (var c in _temporary_buffer.ToString()) {
            emit_character_token(c);
          }
          reconsume_in_state(State.RCDATA);
          break;
        case State.RAWTEXTLessThanSign:
          consume_next_input_character();
          if (_current_input_character == '/') {
            _temporary_buffer = new StringBuilder();
            switch_to(State.RAWTEXTEndTagOpen);
          } else {
            emit_character_token('<');
            reconsume_in_state(State.RAWTEXT);
          }
          break;
        case State.RAWTEXTEndTagOpen:
          consume_next_input_character();
          if (is_ascii_alpha(_current_input_character)) {
            _current_token = new HTMLToken(HTMLToken.Type.EndTag);
            reconsume_in_state(State.RAWTEXTEndTagName);
          } else {
            emit_character_token('<');
            reconsume_in_state(State.RAWTEXT);
          }
          break;
        case State.RAWTEXTEndTagName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.BeforeAttributeName);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '/') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.SelfClosingStartTag);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '>') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.Data);
              emit_current_token();
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(to_lower(current_input_character));
            append_to_temp_buffer(current_input_character);
            continue;
          } else if (is_ascii_lower_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(current_input_character);
            append_to_temp_buffer(current_input_character);
            continue;
          }

          emit_character_token('<');
          emit_character_token('/');
          Debug.Assert(_temporary_buffer != null);
          foreach (var c in _temporary_buffer.ToString()) {
            emit_character_token(c);
          }
          reconsume_in_state(State.RAWTEXT);
          break;
        case State.ScriptDataLessThanSign:
          consume_next_input_character();
          if (_current_input_character == '/') {
            _temporary_buffer = new StringBuilder();
            switch_to(State.ScriptDataEndTagOpen);
          } else if (_current_input_character == '!') {
            switch_to(State.ScriptDataEscapeStart);
            emit_character_token('<');
            emit_character_token('!');
          } else {
            emit_character_token('<');
            reconsume_in_state(State.ScriptData);
          }
          break;
        case State.ScriptDataEndTagOpen:
          consume_next_input_character();
          if (is_ascii_alpha(_current_input_character)) {
            _current_token = new HTMLToken(HTMLToken.Type.EndTag);
            reconsume_in_state(State.ScriptDataEndTagName);
          } else {
            emit_character_token('<');
            emit_character_token('/');
            reconsume_in_state(State.ScriptData);
          }
          break;
        case State.ScriptDataEndTagName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.BeforeAttributeName);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '/') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.SelfClosingStartTag);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '>') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.Data);
              emit_current_token();
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(to_lower(current_input_character));
            append_to_temp_buffer(current_input_character);
            continue;
          } else if (is_ascii_lower_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(current_input_character);
            append_to_temp_buffer(current_input_character);
            continue;
          }

          emit_character_token('<');
          emit_character_token('/');
          Debug.Assert(_temporary_buffer != null);
          foreach (var c in _temporary_buffer.ToString()) {
            emit_character_token(c);
          }
          reconsume_in_state(State.ScriptData);
          break;
        case State.ScriptDataEscapeStart:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.ScriptDataEscapeStartDash);
            emit_character_token('-');
          } else {
            reconsume_in_state(State.ScriptData);
          }
          break;
        case State.ScriptDataEscapeStartDash:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.ScriptDataEscapeDashDash);
            emit_character_token('-');
          } else {
            reconsume_in_state(State.ScriptData);
          }
          break;
        case State.ScriptDataEscaped:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.ScriptDataEscapeDash);
            emit_character_token('-');
          } else if (_current_input_character == '<') {
            switch_to(State.ScriptDataEscapedLessThanSign);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            emit_end_of_file_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-script-html-comment-like-text");
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.ScriptDataEscapeDash:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.ScriptDataEscapeDashDash);
            emit_character_token('-');
          } else if (_current_input_character == '<') {
            switch_to(State.ScriptDataEscapedLessThanSign);
          } else if (_current_input_character == NULL) {
            parse_error("eof-in-html-comment-like-text");
            emit_end_of_file_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-script-html-comment-like-text");
            emit_end_of_file_token();
          } else {
            switch_to(State.ScriptDataEscaped);
            emit_current_input_character();
          }
          break;
        case State.ScriptDataEscapeDashDash:
          consume_next_input_character();
          if (_current_input_character == '-') {
            emit_character_token('-');
          } else if (_current_input_character == '<') {
            switch_to(State.ScriptDataEscapedLessThanSign);
          } else if (_current_input_character == '>') {
            switch_to(State.ScriptData);
            emit_character_token('>');
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            switch_to(State.ScriptDataEscaped);
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-html-comment-like-text");
            emit_end_of_file_token();
          } else {
            switch_to(State.ScriptDataEscaped);
            emit_current_input_character();
          }
          break;
        case State.ScriptDataEscapedLessThanSign:
          consume_next_input_character();
          if (_current_input_character == '/') {
            _temporary_buffer = new StringBuilder();
            switch_to(State.ScriptDataEndTagOpen);
          } else if (is_ascii_alpha(_current_input_character)) {
            _temporary_buffer = new StringBuilder();
            emit_character_token('<');
            reconsume_in_state(State.ScriptDataDoubleEscapeStart);
          } else {
            emit_character_token('<');
            reconsume_in_state(State.ScriptDataEscaped);
          }
          break;
        case State.ScriptDataEscapedEndTagOpen:
          consume_next_input_character();
          if (is_ascii_alpha(_current_input_character)) {
            _current_token = new HTMLToken(HTMLToken.Type.EndTag);
            reconsume_in_state(State.ScriptDataEscapedEndTagName);
          } else {
            emit_character_token('<');
            emit_character_token('/');
            reconsume_in_state(State.ScriptDataEscaped);
          }
          break;
        case State.ScriptDataEscapedEndTagName:
          if (is_white_space(_current_input_character)) {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.BeforeAttributeName);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '/') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.SelfClosingStartTag);
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (_current_input_character == '>') {
            if (is_appropriate_end_tag_token(_current_token)) {
              switch_to(State.Data);
              emit_current_token();
              continue;
            } else {
              // treat it as per the "anything else" entry below.
            }
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(to_lower(current_input_character));
            append_to_temp_buffer(current_input_character);
            continue;
          } else if (is_ascii_lower_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_name(current_input_character);
            append_to_temp_buffer(current_input_character);
            continue;
          }

          emit_character_token('<');
          emit_character_token('/');
          Debug.Assert(_temporary_buffer != null);
          foreach (var c in _temporary_buffer.ToString()) {
            emit_character_token(c);
          }
          reconsume_in_state(State.ScriptDataEscaped);
          break;
        case State.ScriptDataDoubleEscapeStart:
          consume_next_input_character();
          if (is_white_space(_current_input_character) || _current_input_character == '/' || _current_input_character == '>') {
            Debug.Assert(_temporary_buffer != null);
            if (_temporary_buffer.ToString() == "script") {
              switch_to(State.ScriptDataDoubleEscaped);
            } else {
              switch_to(State.ScriptDataEscaped);
              emit_current_input_character();
            }
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            append_to_temp_buffer(to_lower(current_input_character));
            emit_current_input_character();
          } else if (is_ascii_lower_alpha(_current_input_character)) {
            append_to_temp_buffer(current_input_character);
            emit_current_input_character();
          } else {
            reconsume_in_state(State.ScriptDataEscaped);
          }
          break;
        case State.ScriptDataDoubleEscaped:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.ScriptDataDoubleEscapedDash);
            emit_character_token('-');
          } else if (_current_input_character == '<') {
            switch_to(State.ScriptDataDoubleEscapedLessThanSign);
            emit_character_token('<');
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-html-comment-like-text");
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.ScriptDataDoubleEscapedDash:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.ScriptDataDoubleEscapedDashDash);
            emit_character_token('-');
          } else if (_current_input_character == '<') {
            switch_to(State.ScriptDataDoubleEscapedLessThanSign);
            emit_character_token('<');
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            switch_to(State.ScriptDataDoubleEscaped);
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-html-comment-like-text");
            emit_end_of_file_token();
          } else {
            switch_to(State.ScriptDataDoubleEscaped);
            emit_current_input_character();
          }
          break;
        case State.ScriptDataDoubleEscapedDashDash:
          consume_next_input_character();
          if (_current_input_character == '-') {
            emit_character_token('-');
          } else if (_current_input_character == '<') {
            switch_to(State.ScriptDataDoubleEscapedLessThanSign);
            emit_character_token('<');
          } else if (_current_input_character == '>') {
            switch_to(State.ScriptData);
            emit_character_token('>');
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            switch_to(State.ScriptDataDoubleEscaped);
            emit_character_token(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-html-comment-like-text");
            emit_end_of_file_token();
          } else {
            switch_to(State.ScriptDataDoubleEscaped);
            emit_current_input_character();
          }
          break;
        case State.ScriptDataDoubleEscapedLessThanSign:
          consume_next_input_character();
          if (_current_input_character == '/') {
            _temporary_buffer = new StringBuilder();
            switch_to(State.ScriptDataDoubleEscapedEnd);
            emit_character_token('/');
          } else {
            reconsume_in_state(State.ScriptDataDoubleEscaped);
          }
          break;
        case State.ScriptDataDoubleEscapedEnd:
          consume_next_input_character();
          if (is_white_space(_current_input_character) || _current_input_character == '/' || _current_input_character == '>') {
            Debug.Assert(_temporary_buffer != null);
            if (_temporary_buffer.ToString() == "script") {
              switch_to(State.ScriptDataEscaped);
            } else {
              switch_to(State.ScriptDataDoubleEscaped);
              emit_current_input_character();
            }
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            append_to_temp_buffer(to_lower(current_input_character));
            emit_current_input_character();
          } else if (is_ascii_lower_alpha(_current_input_character)) {
            append_to_temp_buffer(current_input_character);
            emit_current_input_character();
          } else {
            reconsume_in_state(State.ScriptDataDoubleEscaped);
          }
          break;
        case State.BeforeAttributeName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
          } else if (_current_input_character == '/' || _current_input_character == '>' || _current_input_character == EOF) {
            reconsume_in_state(State.AfterAttributeName);
          } else if (_current_input_character == '=') {
            parse_error("unexpected-question-mark-instead-of-tag-name");
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.start_new_attribute(current_input_character, "");
            switch_to(State.AttributeName);
          } else {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.start_new_attribute();
            reconsume_in_state(State.AttributeName);
          }
          break;
        case State.AttributeName:
          consume_next_input_character();
          if (is_white_space(_current_input_character) || _current_input_character == '/' || _current_input_character == '>' || _current_input_character == EOF) {
            reconsume_in_state(State.AfterAttributeName);
          } else if (_current_input_character == '=') {
            switch_to(State.BeforeAttributeValue);
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_name(to_lower(current_input_character));
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-question-mark-instead-of-tag-name");
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_name(REPLACEMENT_CHARACTER);
          } else {
            if (_current_input_character == '"' || _current_input_character == '\'' || _current_input_character == '<') {
              parse_error("unexpected-character-in-attribute-name");
            }

            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_name(current_input_character);
          }
          break;
        case State.AfterAttributeName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
          } else if (_current_input_character == '/') {
            switch_to(State.SelfClosingStartTag);
          } else if (_current_input_character == '=') {
            switch_to(State.BeforeAttributeValue);
          } else if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.tag != null);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-tag");
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.start_new_attribute();
            reconsume_in_state(State.AttributeName);
          }
          break;
        case State.BeforeAttributeValue:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
          } else if (_current_input_character == '"') {
            switch_to(State.AttributeValueDoubleQuoted);
          } else if (_current_input_character == '\'') {
            switch_to(State.AttributeValueSingleQuoted);
          } else if (_current_input_character == '>') {
            parse_error("missing-attribute-value");
            switch_to(State.Data);
            Debug.Assert(_current_token?.tag != null);
            emit_current_token();
          } else {
            reconsume_in_state(State.AttributeValueUnquoted);
          }
          break;
        case State.AttributeValueDoubleQuoted:
          consume_next_input_character();
          if (_current_input_character == '"') {
            switch_to(State.AfterAttributeValueQuoted);
          } else if (_current_input_character == '&') {
            _return_state = State.AttributeValueDoubleQuoted;
            switch_to(State.CharacterReference);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_value(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-tag");
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_value(current_input_character);
          }
          break;
        case State.AttributeValueSingleQuoted:
          consume_next_input_character();
          if (_current_input_character == '\'') {
            switch_to(State.AfterAttributeValueQuoted);
          } else if (_current_input_character == '&') {
            _return_state = State.AttributeValueSingleQuoted;
            switch_to(State.CharacterReference);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_value(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-tag");
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_value(current_input_character);
          }
          break;
        case State.AttributeValueUnquoted:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.BeforeAttributeName);
          } else if (_current_input_character == '&') {
            _return_state = State.AttributeValueUnquoted;
            switch_to(State.CharacterReference);
          } else if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.tag != null);
            emit_current_token();
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-character-in-attribute-name");
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_value(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == '\"' || _current_input_character == '\'' || _current_input_character == '<' || _current_input_character == '=' || _current_input_character == '`') {
            parse_error("unexpected-character-in-attribute-name");
            emit_end_of_file_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-tag");
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.append_to_current_attribute_value(current_input_character);
          }
          break;
        case State.AfterAttributeValueQuoted:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.BeforeAttributeName);
          } else if (_current_input_character == '/') {
            switch_to(State.SelfClosingStartTag);
          } else if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.tag != null);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-tag");
            emit_end_of_file_token();
          } else {
            parse_error("missing-whitespace-between-attributes");
            reconsume_in_state(State.BeforeAttributeName);
          }
          break;
        case State.SelfClosingStartTag:
          consume_next_input_character();
          if (_current_input_character == '>') {
            Debug.Assert(_current_token?.tag != null);
            _current_token.tag.self_closing = true;
            switch_to(State.Data);
          }
          break;
        case State.BogusComment:
          consume_next_input_character();
          if (_current_input_character == '>') {
            switch_to(State.Data);
          } else if (_current_input_character == EOF) {
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            emit_current_token();
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            _current_token.comment_or_character.append_to_data(REPLACEMENT_CHARACTER);
          } else {
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            _current_token.comment_or_character.append_to_data((int)_current_input_character!);
          }
          break;
        case State.MarkupDeclarationOpen:
          if (try_consume("--")) {
            _current_token = new HTMLToken(HTMLToken.Type.Comment);
            switch_to(State.CommentStart);
          } else if (try_consume("DOCTYPE")) {
            switch_to(State.DOCTYPE);
          } else if (try_consume("[CDATA[")) {
            // FIXME: If there is an adjusted current node and it is not an element in the HTML namespace, then switch to the CDATA section state.
            // if (_adjusted_current_node != null && !is_in_the_HTML_namespace()) {
            //   switch_to(State.CDATASection);
            // } else {
            parse_error("cdata-in-html-content");
            _current_token = new HTMLToken(HTMLToken.Type.Comment);
            _current_token.comment_or_character!.append_to_data("[CDATA[");
            switch_to(State.BogusComment);
            // }
          } else {
            parse_error("incorrectly-opened-comment");
            _current_token = new HTMLToken(HTMLToken.Type.Comment);
            switch_to(State.BogusComment);
          }
          break;
        case State.CommentStart:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.CommentStartDashState);
          } else if (_current_input_character == '>') {
            parse_error("abrupt-closing-of-empty-comment");
            switch_to(State.Data);
            Debug.Assert(_current_token?.type != HTMLToken.Type.Comment);
            emit_current_token();
          } else {
            reconsume_in_state(State.Comment);
          }
          break;
        case State.CommentStartDashState:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.CommentEnd);
          } else if (_current_input_character == '>') {
            parse_error("abrupt-closing-of-empty-comment");
            switch_to(State.Data);
            Debug.Assert(_current_token?.type != HTMLToken.Type.Comment);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-comment");
            Debug.Assert(_current_token?.type != HTMLToken.Type.Comment);
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.comment_or_character != null);
            _current_token.comment_or_character.append_to_data('-');
            reconsume_in_state(State.Comment);
          }
          break;
        case State.Comment:
          consume_next_input_character();
          if (_current_input_character == '<') {
            Debug.Assert(_current_token?.comment_or_character != null);
            _current_token.comment_or_character.append_to_data(current_input_character);
            switch_to(State.CommentLessThanSign);
          } else if (_current_input_character == '-') {
            switch_to(State.CommentEndDash);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.comment_or_character != null);
            _current_token.comment_or_character.append_to_data(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-comment");
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.comment_or_character?.data != null);
            _current_token.comment_or_character.append_to_data(current_input_character);
          }
          break;
        case State.CommentLessThanSign:
          consume_next_input_character();
          if (_current_input_character == '!') {
            Debug.Assert(_current_token?.comment_or_character?.data != null);
            _current_token.comment_or_character.append_to_data(current_input_character);
            switch_to(State.CommentLessThanSignBang);
          } else if (_current_input_character == '<') {
            Debug.Assert(_current_token?.comment_or_character?.data != null);
            _current_token.comment_or_character.append_to_data(current_input_character);
          } else {
            reconsume_in_state(State.Comment);
          }
          break;
        case State.CommentLessThanSignBang:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.CommentLessThanSignBangDash);
          } else {
            reconsume_in_state(State.CommentEndDash);
          }
          break;
        case State.CommentLessThanSignBangDash:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.CommentLessThanSignBangDashDash);
          } else {
            reconsume_in_state(State.CommentEndDash);
          }
          break;
        case State.CommentLessThanSignBangDashDash:
          consume_next_input_character();
          if (_current_input_character == '>' || _current_input_character == EOF) {
            reconsume_in_state(State.CommentEnd);
          } else {
            parse_error("nested-comment");
            reconsume_in_state(State.CommentEnd);
          }
          break;
        case State.CommentEndDash:
          consume_next_input_character();
          if (_current_input_character == '-') {
            switch_to(State.CommentEnd);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-comment");
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.comment_or_character?.data != null);
            _current_token.comment_or_character.append_to_data('-');
            reconsume_in_state(State.Comment);
          }
          break;
        case State.CommentEnd:
          consume_next_input_character();
          if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            emit_current_token();
          } else if (_current_input_character == '!') {
            switch_to(State.CommentEndBang);
          } else if (_current_input_character == '-') {
            Debug.Assert(_current_token?.comment_or_character != null);
            _current_token.comment_or_character.append_to_data('-');
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-comment");
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.comment_or_character?.data != null);
            _current_token.comment_or_character.append_to_data('-');
            reconsume_in_state(State.Comment);
          }
          break;
        case State.CommentEndBang:
          consume_next_input_character();
          if (_current_input_character == '-') {
            Debug.Assert(_current_token?.comment_or_character != null);
            _current_token.comment_or_character.append_to_data("--!");
            switch_to(State.CommentEnd);
          } else if (_current_input_character == '>') {
            parse_error("incorrectly-closed-comment");
            switch_to(State.Data);
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-comment");
            Debug.Assert(_current_token?.type == HTMLToken.Type.Comment);
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.comment_or_character?.data != null);
            _current_token.comment_or_character.append_to_data("--!");
            reconsume_in_state(State.Comment);
          }
          break;
        case State.DOCTYPE:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.BeforeDOCTYPEName);
          } else if (_current_input_character == '>') {
            reconsume_in_state(State.BeforeDOCTYPEName);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            _current_token = new HTMLToken(HTMLToken.Type.DOCTYPE);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            parse_error("missing-whitespace-before-doctype-name");
            reconsume_in_state(State.BeforeDOCTYPEName);
          }
          break;
        case State.BeforeDOCTYPEName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            // Ignore the character.
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            _current_token = new HTMLToken(HTMLToken.Type.DOCTYPE);
            _current_token.doctype.append_to_name(to_lower(current_input_character));
            switch_to(State.DOCTYPEName);
          } else if (_current_input_character == EOF) {
            parse_error("unexpected-null-character");
            _current_token = new HTMLToken(HTMLToken.Type.DOCTYPE);
            _current_token.doctype.append_to_name(REPLACEMENT_CHARACTER);
            emit_current_token();
          } else if (_current_input_character == '>') {
            parse_error("missing-doctype-name");

            _current_token = new HTMLToken(HTMLToken.Type.DOCTYPE);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            switch_to(State.Data);
          } else {
            _current_token = new HTMLToken(HTMLToken.Type.DOCTYPE);
            _current_token.doctype.append_to_name(current_input_character);
            switch_to(State.DOCTYPEName);
          }
          break;
        case State.DOCTYPEName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.AfterDOCTYPEName);
          } else if (_current_input_character == '>') {
            Debug.Assert(_current_token?.type == HTMLToken.Type.DOCTYPE);
            switch_to(State.Data);
            emit_current_token();
          } else if (is_ascii_upper_alpha(_current_input_character)) {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.append_to_name(current_input_character);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.append_to_name(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            _current_token = new HTMLToken(HTMLToken.Type.DOCTYPE);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.append_to_name(current_input_character);
          }
          break;
        case State.AfterDOCTYPEName:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            // Ignore the character.
          } else if (_current_input_character == '>') {
            emit_current_token();
            switch_to(State.Data);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            if (try_consume("PUBLIC")) {
              switch_to(State.AfterDOCTYPEPublicKeyword);
            } else if (try_consume("SYSTEM")) {
              switch_to(State.AfterDOCTYPESystemKeyword);
            } else {
              parse_error("invalid-character-sequence-after-doctype-name");
              Debug.Assert(_current_token?.doctype != null);
              _current_token.doctype.force_quirks = true;
              reconsume_in_state(State.BogusDOCTYPE);
            }
          }
          break;
        case State.AfterDOCTYPEPublicKeyword:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.BeforeDOCTYPEPublicIdentifier);
          } else if (_current_input_character == '"') {
            parse_error("missing-whitespace-after-doctype-public-keyword");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_public_identifier();
            switch_to(State.DOCTYPEPublicIdentifierDoubleQuoted);
          } else if (_current_input_character == '\'') {
            parse_error("missing-whitespace-after-doctype-public-keyword");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_public_identifier();
            switch_to(State.DOCTYPEPublicIdentifierSingleQuoted);
          } else if (_current_input_character == '>') {
            parse_error("missing-doctype-public-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            switch_to(State.Data);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          }
          break;
        case State.BeforeDOCTYPEPublicIdentifier:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
          } else if (_current_input_character == '"') {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_public_identifier();
            switch_to(State.DOCTYPEPublicIdentifierDoubleQuoted);
          } else if (_current_input_character == '\'') {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_public_identifier();
            switch_to(State.DOCTYPEPublicIdentifierSingleQuoted);
          } else if (_current_input_character == '>') {
            parse_error("missing-doctype-public-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            parse_error("missing-quote-before-doctype-public-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            reconsume_in_state(State.BogusDOCTYPE);
          }
          break;
        case State.DOCTYPEPublicIdentifierDoubleQuoted:
          consume_next_input_character();
          if (_current_input_character == '"') {
            switch_to(State.AfterDOCTYPEPublicIdentifier);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.doctype?.public_identifier != null);
            _current_token.doctype.append_to_public_identifier(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == '>') {
            parse_error("abrupt-doctype-public-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            switch_to(State.Data);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.append_to_public_identifier(current_input_character);
          }
          break;
        case State.DOCTYPEPublicIdentifierSingleQuoted:
          consume_next_input_character();
          if (_current_input_character == '\'') {
            switch_to(State.AfterDOCTYPEPublicIdentifier);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.append_to_public_identifier(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == '>') {
            parse_error("abrupt-doctype-public-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            switch_to(State.Data);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.append_to_public_identifier(current_input_character);
          }
          break;
        case State.AfterDOCTYPEPublicIdentifier:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.BetweenDOCTYPEPublicAndSystemIdentifier);
          } else if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.type == HTMLToken.Type.DOCTYPE);
            emit_current_token();
          } else if (_current_input_character == '"') {
            parse_error("missing-whitespace-between-doctype-public-and-system-identifiers");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierDoubleQuoted);
          } else if (_current_input_character == '\'') {
            parse_error("missing-whitespace-between-doctype-public-and-system-identifiers");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierSingleQuoted);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            parse_error("missing-quote-before-doctype-system-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            reconsume_in_state(State.BogusDOCTYPE);
          }
          break;
        case State.BetweenDOCTYPEPublicAndSystemIdentifier:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
          } else if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.type == HTMLToken.Type.DOCTYPE);
            emit_current_token();
          } else if (_current_input_character == '"') {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierDoubleQuoted);
          } else if (_current_input_character == '\'') {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierSingleQuoted);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            parse_error("missing-quote-before-doctype-system-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            reconsume_in_state(State.BogusDOCTYPE);
          }
          break;
        case State.AfterDOCTYPESystemKeyword:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
            switch_to(State.BeforeDOCTYPESystemIdentifier);
          } else if (_current_input_character == '"') {
            parse_error("missing-whitespace-after-doctype-system-keyword");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierDoubleQuoted);
          } else if (_current_input_character == '\'') {
            parse_error("missing-whitespace-after-doctype-system-keyword");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierSingleQuoted);
          } else if (_current_input_character == '>') {
            parse_error("missing-doctype-system-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            switch_to(State.Data);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            parse_error("missing-doctype-system-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            reconsume_in_state(State.BogusDOCTYPE);
          }
          break;
        case State.BeforeDOCTYPESystemIdentifier:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
          } else if (_current_input_character == '"') {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierDoubleQuoted);
          } else if (_current_input_character == '\'') {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.init_system_identifier();
            switch_to(State.DOCTYPESystemIdentifierSingleQuoted);
          } else if (_current_input_character == '>') {
            parse_error("missing-doctype-system-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            switch_to(State.Data);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            parse_error("missing-quote-before-doctype-system-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            reconsume_in_state(State.BogusComment);
          }
          break;
        case State.DOCTYPESystemIdentifierDoubleQuoted:
          consume_next_input_character();
          if (_current_input_character == '"') {
            switch_to(State.AfterDOCTYPESystemIdentifier);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.doctype?.system_identifier != null);
            _current_token.doctype.append_to_system_identifier(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == '>') {
            parse_error("abrupt-doctype-public-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            switch_to(State.Data);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.append_to_system_identifier(current_input_character);
          }
          break;
        case State.DOCTYPESystemIdentifierSingleQuoted:
          consume_next_input_character();
          if (_current_input_character == '\'') {
            switch_to(State.AfterDOCTYPESystemIdentifier);
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
            Debug.Assert(_current_token?.doctype?.system_identifier != null);
            _current_token.doctype.append_to_system_identifier(REPLACEMENT_CHARACTER);
          } else if (_current_input_character == '>') {
            parse_error("abrupt-doctype-public-identifier");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            switch_to(State.Data);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            Debug.Assert(_current_token?.doctype?.system_identifier != null);
            _current_token.doctype.append_to_system_identifier(current_input_character);
          }
          break;
        case State.AfterDOCTYPESystemIdentifier:
          consume_next_input_character();
          if (is_white_space(_current_input_character)) {
          } else if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.type == HTMLToken.Type.DOCTYPE);
            emit_current_token();
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-doctype");
            Debug.Assert(_current_token?.doctype != null);
            _current_token.doctype.force_quirks = true;
            emit_current_token();
            emit_end_of_file_token();
          } else {
            parse_error("unexpected-character-after-doctype-system-identifier");
            reconsume_in_state(State.BogusDOCTYPE);
          }
          break;
        case State.BogusDOCTYPE:
          consume_next_input_character();
          if (_current_input_character == '>') {
            switch_to(State.Data);
            Debug.Assert(_current_token?.type == HTMLToken.Type.DOCTYPE);
            emit_current_token();
          } else if (_current_input_character == NULL) {
            parse_error("unexpected-null-character");
          } else if (_current_input_character == EOF) {
            Debug.Assert(_current_token?.type == HTMLToken.Type.DOCTYPE);
            emit_current_token();
            emit_end_of_file_token();
          } else {
          }
          break;
        case State.CDATASection:
          consume_next_input_character();
          if (_current_input_character == '!') {
            switch_to(State.CDATASectionBracket);
          } else if (_current_input_character == EOF) {
            parse_error("eof-in-cdata");
            emit_end_of_file_token();
          } else {
            emit_current_input_character();
          }
          break;
        case State.CDATASectionBracket:
          consume_next_input_character();
          if (_current_input_character == ']') {
            switch_to(State.CDATASectionEnd);
          } else {
            emit_character_token(']');
            reconsume_in_state(State.CDATASection);
          }
          break;
        case State.CDATASectionEnd:
          consume_next_input_character();
          if (_current_input_character == ']') {
            emit_character_token(']');
          } else if (_current_input_character == '>') {
            switch_to(State.Data);
          } else {
            emit_character_token(']');
            reconsume_in_state(State.CDATASection);
          }
          break;
        case State.CharacterReference:
          _temporary_buffer = new StringBuilder("&");
          consume_next_input_character();
          if (is_ascii_alphanumeric(_current_input_character)) {
            reconsume_in_state(State.NamedCharacterReference);
          } else if (_current_input_character == '#') {
            append_to_temp_buffer(current_input_character);
            switch_to(State.NumericCharacterReference);
          } else {
            flush_code_points_consumed_as_a_character_reference();
            reconsume_in_state(_return_state);
          }
          break;
        case State.NamedCharacterReference:
          if (consume_named_character_references(out string? name) && name != null) {
            if (character_reference_was_consumed_as_part_of_an_attribute() &&
                name.Last() != ';' &&
                (next_input_character == '=' || is_ascii_alphanumeric(next_input_character))) {
              flush_code_points_consumed_as_a_character_reference();
              switch_to(_return_state);
            } else {
              if (name.Last() != ';') {
                parse_error("missing-semicolon-after-character-reference");
              }
              _temporary_buffer = new StringBuilder(NamedCharacterReferences.get_characters(name));
              flush_code_points_consumed_as_a_character_reference();
              switch_to(_return_state);
            }
          } else {
            flush_code_points_consumed_as_a_character_reference();
            switch_to(State.AmbiguousAmpersand);
          }
          break;
        case State.AmbiguousAmpersand:
          consume_next_input_character();
          if (is_ascii_alpha(_current_input_character)) {
            if (character_reference_was_consumed_as_part_of_an_attribute()) {
              Debug.Assert(_current_token?.tag != null);
              _current_token.tag.append_to_current_attribute_value(current_input_character);
            } else {
              emit_current_input_character();
            }
          } else if (_current_input_character == ';') {
            parse_error("unknown-named-character-reference");
            reconsume_in_state(_return_state);
          } else {
            reconsume_in_state(_return_state);
          }
          break;
        case State.NumericCharacterReference:
          _character_reference_code = 0;
          consume_next_input_character();
          if (_current_input_character == 'x' || _current_input_character == 'X') {
            append_to_temp_buffer(current_input_character);
            switch_to(State.HexadecimalCharacterReferenceStart);
          } else {
            reconsume_in_state(State.DecimalCharacterReferenceStart);
          }
          break;
        case State.HexadecimalCharacterReferenceStart:
          consume_next_input_character();
          if (is_ascii_hex_digit(_current_input_character)) {
            reconsume_in_state(State.HexadecimalCharacterReference);
          } else {
            parse_error("absence-of-digits-in-numeric-character-reference");
            flush_code_points_consumed_as_a_character_reference();
            reconsume_in_state(_return_state);
          }
          break;
        case State.DecimalCharacterReferenceStart:
          consume_next_input_character();
          if (is_ascii_digit(_current_input_character)) {
            reconsume_in_state(State.DecimalCharacterReference);
          } else {
            parse_error("absence-of-digits-in-numeric-character-reference");
            flush_code_points_consumed_as_a_character_reference();
            reconsume_in_state(_return_state);
          }
          break;
        case State.HexadecimalCharacterReference:
          consume_next_input_character();
          if (is_ascii_digit(_current_input_character)) {
            _character_reference_code = _character_reference_code * 16 + (current_input_character - 0x30);
          } else if (is_ascii_upper_hex_digit(_current_input_character)) {
            _character_reference_code = _character_reference_code * 16 + (current_input_character - 0x37);
          } else if (is_ascii_lower_hex_digit(_current_input_character)) {
            _character_reference_code = _character_reference_code * 16 + (current_input_character - 0x57);
          } else if (_current_input_character == ';') {
            switch_to(State.NumericCharacterReferenceEnd);
          } else {
            parse_error("missing-semicolon-after-character-reference");
            reconsume_in_state(State.NumericCharacterReferenceEnd);
          }
          break;
        case State.DecimalCharacterReference:
          consume_next_input_character();
          if (is_ascii_digit(_current_input_character)) {
            _character_reference_code = _character_reference_code * 10 + (current_input_character - 0x30);
          } else if (_current_input_character == ';') {
            switch_to(State.NumericCharacterReferenceEnd);
          } else {
            parse_error("missing-semicolon-after-character-reference");
            reconsume_in_state(State.NumericCharacterReferenceEnd);
          }
          break;
        case State.NumericCharacterReferenceEnd:
          if (_character_reference_code == 0) {
            parse_error("null-character-reference");
            _character_reference_code = (int)REPLACEMENT_CHARACTER;
          } else if (_character_reference_code > 0x10FFFF) {
            parse_error("character-reference-outside-unicode-range");
            _character_reference_code = (int)REPLACEMENT_CHARACTER;
          } else if (is_surrogate(_character_reference_code)) {
            parse_error("surrogate-character-reference");
            _character_reference_code = (int)REPLACEMENT_CHARACTER;
          } else if (is_noncharacter(_character_reference_code)) {
            parse_error("noncharacter-character-reference");
            _character_reference_code = (int)REPLACEMENT_CHARACTER;
          } else if (_character_reference_code == 0x0D || is_control(_character_reference_code) && !is_ascii_whitespace(_character_reference_code)) {
            parse_error("control-character-reference");
          } else if (NumericCharacterReferenceTable.data.ContainsKey(_character_reference_code)) {
            _character_reference_code = NumericCharacterReferenceTable.data[_character_reference_code];
          }
          _temporary_buffer = new StringBuilder((char)_character_reference_code);
          flush_code_points_consumed_as_a_character_reference();
          switch_to(_return_state);
          break;
        default:
          throw new Exception($"HTMLTokenizer: unknown state '{_state.ToString()}'");
      }
    }
  }

  string get_named_character_references_characters(string name) {
    return NamedCharacterReferences.get_characters(name);
  }

  void flush_code_points_consumed_as_a_character_reference() {
    Debug.Assert(_temporary_buffer != null);
    if (character_reference_was_consumed_as_part_of_an_attribute()) {
      Debug.Assert(_current_token?.tag != null);
      foreach (var c in _temporary_buffer.ToString()) {
        _current_token.tag.append_to_current_attribute_value(c);
      }
    } else {
      foreach (var c in _temporary_buffer.ToString()) {
        emit_character_token(c);
      }
    }
  }

  bool character_reference_was_consumed_as_part_of_an_attribute() {
    return _return_state == State.AttributeValueDoubleQuoted || _return_state == State.AttributeValueSingleQuoted || _return_state == State.AttributeValueUnquoted;
  }

  // An appropriate end tag token is an end tag token whose tag name matches the tag name of the last start tag to have been emitted from this tokenizer, if any. If no start tag has been emitted from this tokenizer, then no end tag token is appropriate.
  bool is_appropriate_end_tag_token(HTMLToken? token) {
    if (token == null) return false;
    if (token.type != HTMLToken.Type.EndTag) return false;
    Debug.Assert(token.tag != null);
    var last_start_tag_name = get_last_start_tag_name();
    if (last_start_tag_name == null) return false;
    return token.tag.name == last_start_tag_name;
  }

  string? get_last_start_tag_name() {
    var token = _emitted_tokens.FindLast(token => token.type == HTMLToken.Type.StartTag);
    return token?.tag?.name;
  }

  const int REPLACEMENT_CHARACTER = 0xFFFD;
  const int NULL = 0;

#pragma warning disable IDE1006
  static readonly int? EOF = null;
#pragma warning restore IDE1006

  static bool is_leading_surrogate(int? codepoint) {
    return 0xD800 <= codepoint && codepoint <= 0xDBFF;
  }

  static bool is_trailing_surrogate(int? codepoint) {
    return 0xDC00 <= codepoint && codepoint <= 0xDFFF;
  }

  static bool is_surrogate(int? codepoint) {
    return is_leading_surrogate(codepoint) || is_trailing_surrogate(codepoint);
  }

  static bool is_noncharacter(int? codepoint) {
    // A noncharacter is a code point that is in the range U+FDD0 to U+FDEF, inclusive, or
    // U+FFFE, U+FFFF, U+1FFFE, U+1FFFF, U+2FFFE, U+2FFFF, U+3FFFE, U+3FFFF, U+4FFFE, U+4FFFF, U+5FFFE, U+5FFFF, U+6FFFE, U+6FFFF, U+7FFFE, U+7FFFF, U+8FFFE, U+8FFFF, U+9FFFE, U+9FFFF, U+AFFFE, U+AFFFF, U+BFFFE, U+BFFFF, U+CFFFE, U+CFFFF, U+DFFFE, U+DFFFF, U+EFFFE, U+EFFFF, U+FFFFE, U+FFFFF, U+10FFFE, or U+10FFFF.
    return (0xFDD0 <= codepoint && codepoint <= 0xFDEF)
      || codepoint == 0xFFFE
      || codepoint == 0xFFFF
      || codepoint == 0x1FFFE
      || codepoint == 0x1FFFF
      || codepoint == 0x2FFFE
      || codepoint == 0x2FFFF
      || codepoint == 0x3FFFE
      || codepoint == 0x3FFFF
      || codepoint == 0x4FFFE
      || codepoint == 0x4FFFF
      || codepoint == 0x5FFFE
      || codepoint == 0x5FFFF
      || codepoint == 0x6FFFE
      || codepoint == 0x6FFFF
      || codepoint == 0x7FFFE
      || codepoint == 0x7FFFF
      || codepoint == 0x8FFFE
      || codepoint == 0x8FFFF
      || codepoint == 0x9FFFE
      || codepoint == 0x9FFFF
      || codepoint == 0xAFFFE
      || codepoint == 0xAFFFF
      || codepoint == 0xBFFFE
      || codepoint == 0xBFFFF
      || codepoint == 0xCFFFE
      || codepoint == 0xCFFFF
      || codepoint == 0xDFFFE
      || codepoint == 0xDFFFF
      || codepoint == 0xEFFFE
      || codepoint == 0xEFFFF
      || codepoint == 0xFFFFE
      || codepoint == 0xFFFFF
      || codepoint == 0x10FFFE
      || codepoint == 0x10FFFF;
  }


  static bool is_c0_control(int? codepoint) {
    return 0 <= codepoint && codepoint <= 0x001F;
  }

  static bool is_control(int? codepoint) {
    return is_c0_control(codepoint) || 0x007F <= codepoint && codepoint <= 0x009F;
  }

  static bool is_ascii_whitespace(int? codepoint) {
    return codepoint == 0x0009 || codepoint == 0x000A || codepoint == 0x000C || codepoint == 0x000D || codepoint == 0x0020;
  }

  static bool is_ascii_alpha(int? codepoint) {
    return is_ascii_upper_alpha(codepoint) || is_ascii_lower_alpha(codepoint);
  }

  static bool is_ascii_upper_alpha(int? codepoint) {
    return 'A' <= codepoint && codepoint <= 'Z';
  }
  static bool is_ascii_lower_alpha(int? codepoint) {
    return 'a' <= codepoint && codepoint <= 'z';
  }
  static bool is_ascii_alphanumeric(int? codepoint) {
    return is_ascii_digit(codepoint) || is_ascii_lower_alpha(codepoint) || is_ascii_upper_alpha(codepoint);
  }
  static bool is_ascii_upper_hex_digit(int? codepoint) {
    return 'a' <= codepoint && codepoint <= 'f' || 'A' <= codepoint && codepoint <= 'F';
  }
  static bool is_ascii_lower_hex_digit(int? codepoint) {
    return 'a' <= codepoint && codepoint <= 'f' || 'A' <= codepoint && codepoint <= 'F';
  }
  static bool is_ascii_hex_digit(int? codepoint) {
    return is_ascii_upper_alpha(codepoint) || is_ascii_lower_alpha(codepoint);
  }
  static bool is_ascii_digit(int? codepoint) {
    return '0' <= codepoint && codepoint <= '9';
  }
  static bool is_white_space(int? codepoint) {
    return codepoint == '\t' || codepoint == '\n' || codepoint == '\f' || codepoint == ' ';
  }

  static int to_lower(int c) {
    return c + 0x20;
  }

  bool consume_named_character_references(out string? matched) {
    var sb = new StringBuilder();
    matched = null;
    for (var i = 0; i < NamedCharacterReferences.name_max_length(); ++i) {
      var ch = peek_codepoint(i);
      if (ch == EOF) break;

      sb.Append((char)ch!);
      var candidate = sb.ToString();
      if (NamedCharacterReferences.contains(candidate)) {
        matched = candidate;
      }
    }

    if (matched != null) {
      Debug.Assert(_temporary_buffer != null);
      _temporary_buffer.Append(matched);
      consume(matched);
      return true;
    }
    return false;
  }

  void parse_error(string error) {
    if (on_error != null) on_error(error);
  }

  public Action<string>? on_error;

  void reconsume_in_state(State state) {
    Debug.Assert(_cursor > 0);

    _state = state;
    _cursor--;
  }

  void switch_to(State state) {
    _state = state;
  }

  public void switch_to_script_data_state() {
    _state = State.ScriptData;
  }

  bool next_few_characters_are(string str) {
    for (var i = 0; i < str.Length; i++) {
      var c = peek_codepoint(i);
      if (c == null) return false;
      if (to_lower((int)c!) != to_lower(str[i])) return false;
    }
    return true;
  }

  void consume(string str) {
    Debug.Assert(next_few_characters_are(str));

    _cursor += str.Length;
  }

  bool try_consume(string str) {
    if (next_few_characters_are(str)) {
      consume(str);
      return true;
    }
    return false;
  }

  int? peek_codepoint(int offset) {
    if (_cursor + offset >= _input.Length) return EOF;
    return _input[_cursor + offset];
  }

  void consume_next_input_character() {
    if (_cursor < _input.Length) {
      _current_input_character = _input[_cursor++];
    } else {
      _current_input_character = EOF;
    }

    // Console.WriteLine($"[{_state}] _current_input_character: '{(char?)_current_input_character}' next_input_character: '{next_input_character}' ");
  }

  void emit_current_token() {
    Debug.Assert(_current_token != null);

    _emitting_tokens.Add(_current_token);
    _current_token = null;
  }

  void emit_current_input_character() {
    emit_character_token(current_input_character);
  }

  void emit_character_token(int c) {
    _current_token = HTMLToken.create_character_token(c);
    emit_current_token();
  }

  void emit_end_of_file_token() {
    _current_token = new HTMLToken(HTMLToken.Type.EndOfFile);
    emit_current_token();
  }

  enum State {
    Data,
    RCDATA,
    RAWTEXT,
    ScriptData,
    PLAINTEXT,
    TagOpen,
    EndTagOpen,
    TagName,
    RCDATALessThanSign,
    RCDATAEndTagOpen,
    RCDATAEndTagName,
    RAWTEXTLessThanSign,
    RAWTEXTEndTagOpen,
    RAWTEXTEndTagName,
    ScriptDataLessThanSign,
    ScriptDataEndTagOpen,
    ScriptDataEndTagName,
    ScriptDataEscapeStart,
    ScriptDataEscapeStartDash,
    ScriptDataEscaped,
    ScriptDataEscapeDash,
    ScriptDataEscapeDashDash,
    ScriptDataEscapedLessThanSign,
    ScriptDataEscapedEndTagOpen,
    ScriptDataEscapedEndTagName,
    ScriptDataDoubleEscapeStart,
    ScriptDataDoubleEscaped,
    ScriptDataDoubleEscapedDash,
    ScriptDataDoubleEscapedDashDash,
    ScriptDataDoubleEscapedLessThanSign,
    ScriptDataDoubleEscapedEnd,
    BeforeAttributeName,
    AttributeName,
    AfterAttributeName,
    BeforeAttributeValue,
    AttributeValueDoubleQuoted,
    AttributeValueSingleQuoted,
    AttributeValueUnquoted,
    AfterAttributeValueQuoted,
    SelfClosingStartTag,
    BogusComment,
    MarkupDeclarationOpen,
    CommentStart,
    CommentStartDashState,
    Comment,
    CommentLessThanSign,
    CommentLessThanSignBang,
    CommentLessThanSignBangDash,
    CommentLessThanSignBangDashDash,
    CommentEnd,
    CommentEndDash,
    CommentEndBang,
    DOCTYPE,
    BeforeDOCTYPEName,
    DOCTYPEName,
    AfterDOCTYPEName,
    AfterDOCTYPEPublicKeyword,
    BeforeDOCTYPEPublicIdentifier,
    DOCTYPEPublicIdentifierDoubleQuoted,
    DOCTYPEPublicIdentifierSingleQuoted,
    AfterDOCTYPEPublicIdentifier,
    BetweenDOCTYPEPublicAndSystemIdentifier,
    AfterDOCTYPESystemKeyword,
    BeforeDOCTYPESystemIdentifier,
    DOCTYPESystemIdentifierDoubleQuoted,
    DOCTYPESystemIdentifierSingleQuoted,
    AfterDOCTYPESystemIdentifier,
    BogusDOCTYPE,
    CDATASection,
    CDATASectionBracket,
    CDATASectionEnd,
    CharacterReference,
    NamedCharacterReference,
    AmbiguousAmpersand,
    NumericCharacterReference,
    HexadecimalCharacterReferenceStart,
    DecimalCharacterReferenceStart,
    HexadecimalCharacterReference,
    DecimalCharacterReference,
    NumericCharacterReferenceEnd,
  }

  int? next_input_character { get { return peek_codepoint(0); } }

  void append_to_temp_buffer(int codepoint) {
    Debug.Assert(_temporary_buffer != null);
    _temporary_buffer.Append((char)codepoint!);
  }

  int current_input_character { get { return (int)_current_input_character!; } }

  string _input;
  int _cursor = 0;
  int? _current_input_character = null;
  // The state machine must start in the data state.
  State _state = State.Data;
  State _return_state = State.Data;
  HTMLToken? _current_token;
  StringBuilder? _temporary_buffer;
  List<HTMLToken> _emitted_tokens = new List<HTMLToken>();
  List<HTMLToken> _emitting_tokens = new List<HTMLToken>();
  int _character_reference_code;
}
