<script setup lang="ts">
import { ref } from 'vue';
import TheWelcome from '../components/TheWelcome.vue'

let socket: WebSocket | null = null;

const mainProgram = ref('');
const programInput = ref('');
const output = ref('');

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
    const programToExecute = mainProgram.value.replace(/"/g, "'");
    socket.send(`execute-direct "${programToExecute}"`);
  };

  socket.onmessage = (event) => {
    console.log(event.data);
    output.value += "\n" + event.data;
  };

  socket.onerror = (error) => {
    console.error(error);
    output.value += "\nError occurred in WebSocket connection";
  };

  socket.onclose = (event) => {
    console.log('WebSocket connection closed:', event.code, event.reason);
    output.value = "";
    socket = null;
  };
}
</script>

<template>
  <main>
    <textarea v-model="mainProgram" placeholder="Füge hier deinen Simple Script Code ein."></textarea>
    <button @click="start">Starte Ausführung</button>
    <button @click="stop">Stoppe Ausführung</button>
    <input v-model="programInput" placeholder="Programm Input">
    <button @click="sendInput">Sende</button>
    <textarea v-model="output" readonly></textarea>
  </main>
</template>
<style>

</style>
