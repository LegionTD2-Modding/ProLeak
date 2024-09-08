const ProLeak = require('./ProLeak');
const readline = require('readline');

const api = new ProLeak();

function powerfulInterceptor(event, params) {
  if (event === "MethodCall" && params.Method === "SomeSpecificMethod") {
    params.Arguments[0] = "intercepted_value";
    return params;
  } else if (event === "MethodCall" && params.Method === "SomeOtherMethod") {
    return null;
  }
  return params;
}

api.registerInterceptor("MethodCall", powerfulInterceptor);

api.connect()
  .then(() => {
    api.startLeaking();

    const rl = readline.createInterface({
      input: process.stdin,
      output: process.stdout
    });

    function promptUser() {
      rl.question("Enter command (start/stop/quit): ", (command) => {
        switch (command.toLowerCase()) {
          case "start":
            api.startLeaking();
            break;
          case "stop":
            api.stopLeaking();
            break;
          case "quit":
            rl.close();
            api.stopLeaking();
            api.disconnect();
            return;
        }
        promptUser();
      });
    }

    promptUser();
  })
  .catch((error) => {
    console.error(error);
    api.disconnect();
  });
