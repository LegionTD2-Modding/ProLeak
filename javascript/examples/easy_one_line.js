const ProLeak = require('./ProLeak');

// You can use ProLeak with a single line of code
// This will print all events in real time
new ProLeak().plug((event, params) => console.log(`${event}: ${JSON.stringify(params)}`));
