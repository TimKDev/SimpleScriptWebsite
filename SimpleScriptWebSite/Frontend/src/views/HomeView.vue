<script setup lang="ts">
import TheWelcome from '../components/TheWelcome.vue'

let socket: WebSocket | null = null;

function stop() {
  if (!socket) return;
  console.log("Stop WebSocket connection");
  socket.close();
}

function sendInput() {
  if (!socket) return;
  let nameInput = document.getElementById("name") as HTMLInputElement;
  console.log("Send name");
  socket.send(nameInput.value);
}

function start() {
  if (socket) {
    console.log("First stop the socket");
    return;
  }
  console.log('Starting WebSocket connection');
  const protocol = window.location.protocol === 'https:' ? 'wss' : 'ws';
  const wsUrl = `${protocol}://localhost:10000/ws`;
  socket = new WebSocket(wsUrl);

  socket.onopen = () => {
    if (!socket) return;
    console.log('WebSocket connection established');
    socket.send('Hello from the client!');
  };

  socket.onmessage = (event) => {
    console.log('Message from server:', event.data);
  };

  socket.onerror = (error) => {
    console.error('WebSocket error:', error);
  };

  socket.onclose = (event) => {
    console.log('WebSocket connection closed:', event.code, event.reason);
    socket = null;
  };
}
</script>

<template>
  <main>
    <button @click="start">Start Console App</button>
    <button @click="stop">Stop Console App</button>
    <input id="name">
    <button @click="sendInput">Send</button>
  </main>
</template>
