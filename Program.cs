// https://html.spec.whatwg.org/multipage/parsing.html

var contents = File.ReadAllText("./test.html");
var parser = new HTMLParser(contents);
var document = parser.run();
Console.WriteLine(document);

// var tokenizer = new HTMLTokenizer(contents) {
//   on_error = (error) => {
//     Console.ForegroundColor = ConsoleColor.Red;
//     Console.WriteLine(error);
//     Console.WriteLine(new StackTrace(true));
//     Console.ResetColor();
//   }
// };
// HTMLToken token;
// do {
//   token = tokenizer.next_token();
//   Console.ForegroundColor = ConsoleColor.Blue;
//   Console.WriteLine(token.ToString());
//   Console.ResetColor();

//   if (token.type == HTMLToken.Type.StartTag && token.tag.name == "script") {
//     tokenizer.switch_to_script_data_state();
//   }
// } while (!token.is_eof);


