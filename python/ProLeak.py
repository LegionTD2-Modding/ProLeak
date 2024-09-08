#
#     ProLeak API (Python)
#     Copyright (C) 2024  Alexandre 'kidev' Poumaroux
#
#     This program is free software: you can redistribute it and/or modify
#     it under the terms of the GNU General Public License as published by
#     the Free Software Foundation, either version 3 of the License, or
#     (at your option) any later version.
#
#     This program is distributed in the hope that it will be useful,
#     but WITHOUT ANY WARRANTY; without even the implied warranty of
#     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#     GNU General Public License for more details.
#
#     You should have received a copy of the GNU General Public License
#     along with this program.  If not, see <https://www.gnu.org/licenses/>.
#

import socket
import threading
import time
from typing import Callable, Dict, Any, List, Union, Optional
import inspect
import json

class ProLeakConnectionError(Exception):
    """Exception raised when ProLeak fails to connect to the C# engine."""
    pass

class ProLeak:
    def __init__(self, host='localhost', port=69420, connection_timeout=5):
        self.thread = None
        self.host = host
        self.port = port
        self.connection_timeout = connection_timeout
        self.running = False
        self.sharing = False
        self.socket = None
        self.interceptors: Dict[str, List[Callable[..., Optional[Dict[str, Any]]]]] = {}
        self.handlers: Dict[str, List[Callable[..., None]]] = {}

    def connect(self):
        if not self.socket:
            try:
                self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
                self.socket.settimeout(self.connection_timeout)
                self.socket.connect((self.host, self.port))
                self.socket.settimeout(None)  # Reset to blocking mode
                self.running = True
                self.thread = threading.Thread(target=self._read_events)
                self.thread.start()
            except socket.error as e:
                self.socket = None
                raise ProLeakConnectionError(f"Failed to connect to ProLeak Engine: {e}. Is the C# server running?")

    def disconnect(self):
        self.running = False
        if self.socket:
            try:
                self.socket.shutdown(socket.SHUT_RDWR)
            except socket.error:
                pass  # The socket might already be closed
            self.socket.close()
            self.socket = None
        if self.thread and self.thread.is_alive():
            self.thread.join()

    def start_leaking(self):
        if not self.socket:
            raise ProLeakConnectionError("Not connected to ProLeak Engine. Call connect() first.")
        if not self.sharing:
            self._send_command("START")
            self.sharing = True

    def stop_leaking(self):
        if not self.socket:
            raise ProLeakConnectionError("Not connected to ProLeak Engine. Call connect() first.")
        if self.sharing:
            self._send_command("STOP")
            self.sharing = False

    def _send_command(self, command):
        if not self.socket:
            raise ProLeakConnectionError("Not connected to ProLeak Engine. Call connect() first.")
        try:
            self.socket.sendall(command.encode('ascii'))
        except socket.error as e:
            raise ProLeakConnectionError(f"Failed to send command to ProLeak Engine: {e}")

    def _read_events(self):
        buffer = ""
        while self.running:
            try:
                data = self.socket.recv(4096).decode('utf-8')
                if not data:
                    break
                buffer += data
                while "---\n" in buffer:
                    event, buffer = buffer.split("---\n", 1)
                    self._process_event(event.strip().split('\n'))
            except socket.error as e:
                print(f"Socket error: {e}")
                break
        self.disconnect()

    def _process_event(self, event_data):
        event_name = event_data[0].split(': ', 1)[1]
        event_params = json.loads(event_data[1])
        is_prefix = event_params.pop('__is_prefix', False)

        if is_prefix and event_name in self.interceptors:
            for interceptor in self.interceptors[event_name]:
                result = self._execute_interceptor(interceptor, event_name, event_params)
                if result is None:  # Intercept and block
                    self._send_interception_result(event_name, None)
                    return
                elif isinstance(result, dict):  # Intercept and modify
                    event_params = result

            self._send_interception_result(event_name, event_params)
        else:
            for line in event_data[1:]:
                key, value = line.split(': ', 1)
                event_params[key] = value

            def stop_function():
                self.running = False

            # Call specific event handlers
            if event_name in self.event_handlers:
                for handler in self.event_handlers[event_name]:
                    self._call_handler(handler, event_name, event_params, stop_function)

            # Call global handlers
            for handler in self.global_handlers:
                self._call_handler(handler, event_name, event_params, stop_function)

    def _execute_interceptor(self, interceptor, event_name, event_params):
        params = inspect.signature(interceptor).parameters
        if len(params) == 2:
            return interceptor(event_name, event_params)
        elif len(params) == 3:
            return interceptor(event_name, event_params, lambda: None)  # Dummy stop function

    def _send_interception_result(self, event_name, params):
        wrapped_params = {
            "entries": [{"key": k, "value": str(v)} for k, v in params.items()]
        }
        response = json.dumps({
            "event": event_name,
            "params": wrapped_params
        })
        self._send_command(f"INTERCEPTION_RESULT:{response}")

    def register_interceptor(self,
                             events: Union[str, List[str]],
                             interceptor: Callable[..., Optional[Dict[str, Any]]]):
        if isinstance(events, str):
            events = [events]
        for event in events:
            if event not in self.interceptors:
                self.interceptors[event] = []
            self.interceptors[event].append(interceptor)

    def unregister_interceptor(self,
                               events: Union[str, List[str]],
                               interceptor: Callable[..., Optional[Dict[str, Any]]]):
        if isinstance(events, str):
            events = [events]
        for event in events:
            if event in self.interceptors and interceptor in self.interceptors[event]:
                self.interceptors[event].remove(interceptor)

    def _call_handler(self, handler, event_name, event_params, stop_function):
        params = inspect.signature(handler).parameters
        if len(params) == 2:
            handler(event_name, event_params)
        elif len(params) == 3:
            handler(event_name, event_params, stop_function)

    def plug(self, callback: Callable[[str, Dict[str, Any], Callable[[], None]], None]):
        def unplug():
            self.running = False

        try:
            self.connect()
            self.start_leaking()
            self.handlers["__global__"] = [lambda e, p: callback(e, p, unplug)]

            while self.running:
                time.sleep(0.1)  # Small delay to prevent CPU hogging
        except ProLeakConnectionError as e:
            print(f"ProLeak Error: {e}")
        finally:
            self.stop_leaking()
            self.disconnect()

    def register_handler(self,
                         events: Union[str, List[str]],
                         handler: Callable[..., None]):
        if isinstance(events, str):
            events = [events]
        for event in events:
            if event not in self.event_handlers:
                self.event_handlers[event] = []
            self.event_handlers[event].append(handler)

    def unregister_handler(self,
                           events: Union[str, List[str]],
                           handler: Callable[..., None]):
        if isinstance(events, str):
            events = [events]
        for event in events:
            if event in self.event_handlers and handler in self.event_handlers[event]:
                self.event_handlers[event].remove(handler)

    def register_global_handler(self, handler: Callable[..., None]):
        self.global_handlers.append(handler)

    def unregister_global_handler(self, handler: Callable[..., None]):
        if handler in self.global_handlers:
            self.global_handlers.remove(handler)