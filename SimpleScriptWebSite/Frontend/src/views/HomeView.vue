<script setup lang="ts">
import TheWelcome from '../components/TheWelcome.vue'

let socket: WebSocket | null = null;

function stop() {
  console.log("Stop WebSocket connection");
  socket.close();
}

function sendInput() {
  let nameInput = document.getElementById("name") as HTMLInputElement;
  console.log("Send name");
  socket.send(nameInput.value);
}

function start() {
  console.log('Starting WebSocket connection');

  if (socket) {
    socket.close();
  }

  const wsUrl = `ws://localhost:10000/ws`;
  socket = new WebSocket(wsUrl);

  socket.onopen = () => {
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
