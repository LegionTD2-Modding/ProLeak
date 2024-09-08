/*
    ProLeak API (JavaScript)
    Copyright (C) 2024  Alexandre 'kidev' Poumaroux

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

const net = require('net');
const EventEmitter = require('events');

class ProLeakConnectionError extends Error {
  constructor(message) {
    super(message);
    this.name = 'ProLeakConnectionError';
  }
}

class ProLeak extends EventEmitter {
  constructor(host = 'localhost', port = 69420, connectionTimeout = 5000) {
    super();
    this.host = host;
    this.port = port;
    this.connectionTimeout = connectionTimeout;
    this.running = false;
    this.sharing = false;
    this.socket = null;
    this.interceptors = {};
    this.handlers = {};
  }

  connect() {
    return new Promise((resolve, reject) => {
      if (!this.socket) {
        this.socket = new net.Socket();
        this.socket.setTimeout(this.connectionTimeout);

        this.socket.connect(this.port, this.host, () => {
          this.socket.setTimeout(0);
          this.running = true;
          this._readEvents();
          resolve();
        });

        this.socket.on('error', (err) => {
          this.socket = null;
          reject(new ProLeakConnectionError(`Failed to connect to ProLeak Engine: ${err}. Is the C# server running?`));
        });
      } else {
        resolve();
      }
    });
  }

  disconnect() {
    this.running = false;
    if (this.socket) {
      this.socket.destroy();
      this.socket = null;
    }
  }

  startLeaking() {
    if (!this.socket) {
      throw new ProLeakConnectionError("Not connected to ProLeak Engine. Call connect() first.");
    }
    if (!this.sharing) {
      this._sendCommand("START");
      this.sharing = true;
    }
  }

  stopLeaking() {
    if (!this.socket) {
      throw new ProLeakConnectionError("Not connected to ProLeak Engine. Call connect() first.");
    }
    if (this.sharing) {
      this._sendCommand("STOP");
      this.sharing = false;
    }
  }

  _sendCommand(command) {
    if (!this.socket) {
      throw new ProLeakConnectionError("Not connected to ProLeak Engine. Call connect() first.");
    }
    this.socket.write(command);
  }

  _readEvents() {
    let buffer = "";
    this.socket.on('data', (data) => {
      buffer += data.toString('utf-8');
      while (buffer.includes("---\n")) {
        const [event, remaining] = buffer.split("---\n", 2);
        buffer = remaining;
        this._processEvent(event.trim().split('\n'));
      }
    });

    this.socket.on('close', () => {
      this.disconnect();
    });
  }

  _processEvent(eventData) {
    const [eventName, ...paramLines] = eventData;
    const eventParams = JSON.parse(paramLines.join('\n'));
    const isPrefix = eventParams.__is_prefix;
    delete eventParams.__is_prefix;

    if (isPrefix && this.interceptors[eventName]) {
      for (const interceptor of this.interceptors[eventName]) {
        const result = this._executeInterceptor(interceptor, eventName, eventParams);
        if (result === null) {
          this._sendInterceptionResult(eventName, null);
          return;
        } else if (typeof result === 'object') {
          Object.assign(eventParams, result);
        }
      }
      this._sendInterceptionResult(eventName, eventParams);
    } else {
      const stopFunction = () => {
        this.running = false;
      };

      if (this.handlers[eventName]) {
        for (const handler of this.handlers[eventName]) {
          this._callHandler(handler, eventName, eventParams, stopFunction);
        }
      }

      if (this.handlers['__global__']) {
        for (const handler of this.handlers['__global__']) {
          this._callHandler(handler, eventName, eventParams, stopFunction);
        }
      }

      this.emit(eventName, eventParams);
    }
  }

  _executeInterceptor(interceptor, eventName, eventParams) {
    return interceptor(eventName, eventParams);
  }

  _sendInterceptionResult(eventName, params) {
    const wrappedParams = {
      entries: Object.entries(params).map(([key, value]) => ({ key, value: String(value) }))
    };
    const response = JSON.stringify({
      event: eventName,
      params: wrappedParams
    });
    this._sendCommand(`INTERCEPTION_RESULT:${response}`);
  }

  registerInterceptor(events, interceptor) {
    if (typeof events === 'string') {
      events = [events];
    }
    for (const event of events) {
      if (!this.interceptors[event]) {
        this.interceptors[event] = [];
      }
      this.interceptors[event].push(interceptor);
    }
  }

  unregisterInterceptor(events, interceptor) {
    if (typeof events === 'string') {
      events = [events];
    }
    for (const event of events) {
      if (this.interceptors[event]) {
        this.interceptors[event] = this.interceptors[event].filter(i => i !== interceptor);
      }
    }
  }

  _callHandler(handler, eventName, eventParams, stopFunction) {
    handler(eventName, eventParams, stopFunction);
  }

  plug(callback) {
    const unplug = () => {
      this.running = false;
    };

    this.connect()
      .then(() => {
        this.startLeaking();
        this.handlers["__global__"] = [(e, p) => callback(e, p, unplug)];
        
        const checkRunning = () => {
          if (this.running) {
            setTimeout(checkRunning, 100);
          } else {
            this.stopLeaking();
            this.disconnect();
          }
        };
        checkRunning();
      })
      .catch((error) => {
        console.error(`ProLeak Error: ${error}`);
        this.disconnect();
      });
  }

  registerHandler(events, handler) {
    if (typeof events === 'string') {
      events = [events];
    }
    for (const event of events) {
      if (!this.handlers[event]) {
        this.handlers[event] = [];
      }
      this.handlers[event].push(handler);
    }
  }

  unregisterHandler(events, handler) {
    if (typeof events === 'string') {
      events = [events];
    }
    for (const event of events) {
      if (this.handlers[event]) {
        this.handlers[event] = this.handlers[event].filter(h => h !== handler);
      }
    }
  }

  registerGlobalHandler(handler) {
    if (!this.handlers['__global__']) {
      this.handlers['__global__'] = [];
    }
    this.handlers['__global__'].push(handler);
  }

  unregisterGlobalHandler(handler) {
    if (this.handlers['__global__']) {
      this.handlers['__global__'] = this.handlers['__global__'].filter(h => h !== handler);
    }
  }
}

module.exports = ProLeak;
