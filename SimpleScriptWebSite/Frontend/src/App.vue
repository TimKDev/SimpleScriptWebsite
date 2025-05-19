<template>
  <div class="flex h-screen bg-[#1a1b26] text-white">
    <!-- Sidebar -->
    <div class="w-64 bg-[#13141f] p-4">
      <h1 class="text-xl font-bold mb-6">SimpleScript</h1>
      <nav>
        <button
          id="editorTab"
          ref="editorTabButton"
          class="w-full text-left p-2 rounded hover:bg-[#2a2b36] mb-2 flex items-center gap-2"
          aria-label="Try SimpleScript"
          tabindex="0"
          @click="() => handleTabSwitch('editorTab', 'editorSection')"
          @keydown.enter.prevent="() => handleTabSwitch('editorTab', 'editorSection')"
          @keydown.space.prevent="() => handleTabSwitch('editorTab', 'editorSection')"
        >
          <span class="text-purple-400">&lt;/&gt;</span>
          Try SimpleScript
        </button>
        <button
          id="aboutTab"
          ref="aboutTabButton"
          class="w-full text-left p-2 rounded hover:bg-[#2a2b36] flex items-center gap-2"
          aria-label="About"
          tabindex="0"
          @click="() => handleTabSwitch('aboutTab', 'aboutSection')"
          @keydown.enter.prevent="() => handleTabSwitch('aboutTab', 'aboutSection')"
          @keydown.space.prevent="() => handleTabSwitch('aboutTab', 'aboutSection')"
        >
          <span class="text-gray-400">📖</span>
          About
        </button>
      </nav>
    </div>

    <!-- Main Content -->
    <div class="flex-1 p-6 flex flex-col">
      <!-- Editor Section -->
      <div id="editorSection" ref="editorSectionDiv" class="flex flex-col flex-grow">
        <h2 class="text-2xl text-purple-400 mb-4">Code Editor</h2>
        <textarea
          id="codeEditor"
          class="w-full bg-[#1f2028] p-4 rounded-lg font-mono text-green-400 focus:outline-none focus:ring-2 focus:ring-purple-500 mb-4"
          placeholder="// Write your Simple Script code here"
          v-model="code"
          style="height: 60vh;"
        ></textarea>

        <div class="flex justify-between items-center mb-4">
          <div class="flex items-center flex-grow">
            <input
              type="text"
              v-model="programInput"
              placeholder="Enter program input here..."
              class="flex-grow bg-[#1f2028] p-2 rounded-l-lg focus:outline-none focus:ring-2 focus:ring-purple-500 text-white"
            />
            <button
              id="sendInputButton"
              class="bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded-r-lg flex items-center gap-2"
              aria-label="Send Input"
              tabindex="0"
              @click="sendProgramInput"
            >
              <span>➤</span> Send Input
            </button>
          </div>
          <button
            :id="isRunning ? 'stopButton' : 'runButton'"
            :class="[
              'px-4 py-2 rounded-lg flex items-center gap-2 ml-4',
              isRunning
                ? 'bg-red-600 hover:bg-red-700'
                : 'bg-purple-600 hover:bg-purple-700'
            ]"
            :aria-label="isRunning ? 'Stop Connection' : 'Run Code'"
            tabindex="0"
            @click="handleButtonClick"
            @keydown.enter.prevent="handleButtonClick"
            @keydown.space.prevent="handleButtonClick"
          >
            <span v-if="isRunning">⏹</span>
            <span v-else>▶</span>
            {{ isRunning ? 'Stop' : 'Run Code' }}
          </button>
        </div>

        <div
          style="height: 15vh; overflow: auto;"
          class="bg-[#1f2028] p-4 rounded-lg flex flex-col flex-grow min-h-0"
        >
          <h3 class="text-gray-400 mb-2">Output</h3>
          <pre id="outputArea"
               style="flex: 1; overflow: auto;"
               :class="[
                 'font-mono whitespace-pre-wrap',
                 hasErrorOccurred ? 'text-red-400' : 'text-green-400'
               ]"
          >{{ output }}</pre>
        </div>
      </div>

      <!-- About Section -->
      <div id="aboutSection" ref="aboutSectionDiv" class="hidden flex-grow">
        <h2 class="text-2xl text-purple-400 mb-4">About Simple Script</h2>
        <div class="space-y-6">
          <p class="text-gray-300">
            Simple Script is a straightforward programming language designed for simplicity and ease
            of use. Perfect for
            learning programming concepts and building small applications.
          </p>
          <div>
            <h3 class="text-xl text-purple-400 mb-3">Key Features:</h3>
            <ul class="space-y-2 text-gray-300">
              <li>• Simple and intuitive syntax</li>
              <li>• Variables: Declare variables using 'let' keyword</li>
              <li>• Functions: Define functions with 'func' keyword</li>
              <li>• Conditionals: Use 'if', 'else if', and 'else' for control flow</li>
              <li>• Loops: Implement 'while' loops for iteration</li>
            </ul>
          </div>
          <div class="bg-[#2a2b36] p-4 rounded-lg text-gray-300 mt-6">
            To get started, switch to the "Try Simple Script" tab, write your code in the editor,
            and click "Run" to see the output.
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import {ref, onMounted, onUnmounted} from 'vue';

type TabId = 'editorTab' | 'aboutTab';
type SectionId = 'editorSection' | 'aboutSection';

const exampleProgram = `PRINT "How many fibonacci numbers do you want?\\n"
INPUT numsInput
LET nums = ToNumber(numsInput)

IF nums < 0 DO
	PRINT "Number should be greater or equal to zero!"
ENDIF

LET a = 0
LET b = 1
WHILE nums > 0 REPEAT
    PRINT a
    PRINT "\\n"
    LET c = a + b
    LET a = b
    LET b = c
    LET nums = nums - 1
ENDWHILE
`

// Template refs
const editorTabButton = ref<HTMLButtonElement | null>(null);
const aboutTabButton = ref<HTMLButtonElement | null>(null);
const editorSectionDiv = ref<HTMLDivElement | null>(null);
const aboutSectionDiv = ref<HTMLDivElement | null>(null);

// Reactive state
const code = ref('');
const output = ref('');
const programInput = ref('');
const socket = ref<WebSocket | null>(null);
const isRunning = ref(false);
const hasErrorOccurred = ref(false);
const WebSocketReadyState = {OPEN: 1}; // WebSocket.OPEN is 1


onMounted(() => {
  handleTabSwitch('editorTab', 'editorSection');
  code.value = exampleProgram;
});

onUnmounted(() => {
  if (socket.value) {
    socket.value.close();
  }
});

const handleTabSwitch = (activeTabId: TabId, activeSectionId: SectionId) => {
  [editorTabButton.value, aboutTabButton.value].forEach(tab => {
    tab?.classList.remove('bg-[#2a2b36]');
  });
  [editorSectionDiv.value, aboutSectionDiv.value].forEach(section => {
    section?.classList.add('hidden');
  });

  if (activeTabId === 'editorTab') {
    editorTabButton.value?.classList.add('bg-[#2a2b36]');
    editorSectionDiv.value?.classList.remove('hidden');
  } else if (activeTabId === 'aboutTab') {
    aboutTabButton.value?.classList.add('bg-[#2a2b36]');
    aboutSectionDiv.value?.classList.remove('hidden');
  }
};

const handleButtonClick = () => {
  if (isRunning.value) {
    stopWebsocket();
  } else {
    handleRunCode();
  }
}

const handleRunCode = () => {
  isRunning.value = true;
  output.value = "";
  hasErrorOccurred.value = false;

  const protocol = window.location.protocol === 'https:' ? 'wss' : 'ws';
  const wsUrl = `${protocol}://localhost:10000/ws`;

  try {
    socket.value = new WebSocket(wsUrl);

    socket.value.onopen = () => {
      if (!socket.value) {
        // This case should ideally not be reached if onopen is called
        console.error('WebSocket onopen called but socket is null');
        isRunning.value = false; // Reset state
        return;
      }
      const programToExecute = code.value.replace(/"/g, "'");
      socket.value.send(`execute-direct "${programToExecute}"`);
    };

    socket.value.onmessage = (event) => {
      if (event.data.startsWith("output:")) {
        const message = event.data.replace(/output:/g, "");
        if (message.startsWith("Error:")) {
          hasErrorOccurred.value = true;
          output.value += message.replace("Error: ", "");
        } else {
          output.value += message;
        }
      }

      if (event.data.startsWith("error:")) {
        hasErrorOccurred.value = true;
        const message = event.data.replace(/error:/g, "");
        output.value += message;
      }
    };

    socket.value.onerror = (errorEvent) => {
      console.error("WebSocket Error:", errorEvent);
      output.value += "WebSocket connection error. Please ensure the server is running and accessible.\n";
      isRunning.value = false;
      hasErrorOccurred.value = true;
    };

    socket.value.onclose = () => {
      socket.value = null;
      isRunning.value = false;
    };
  } catch (error) {
    console.error("Failed to create WebSocket:", error);
    output.value += `Failed to initialize WebSocket connection: ${error}. Please ensure the server is running and the URL is correct.\n`;
    socket.value = null;
    isRunning.value = false;
    hasErrorOccurred.value = true;
  }
};

const sendProgramInput = () => {
  if (socket.value && socket.value.readyState === WebSocket.OPEN) {
    if (programInput.value.trim() === '') {
      return;
    }
    socket.value.send(programInput.value);
    programInput.value = ''; // Clear input after sending
  }
};

const stopWebsocket = () => {
  if (socket.value) {
    socket.value.close();
  } else {
    isRunning.value = false;
  }
};


</script>

<style scoped>
/* Ensure editor and output areas are scrollable if content overflows */
textarea, pre {
  overflow: auto;
}

/* Minor adjustments for better layout if needed */
.flex-grow {
  flex-grow: 1;
}

.min-h-\[100px\] { /* Tailwind class for min-height, ensure it's correctly applied or defined if custom */
  min-height: 100px;
}

/* Custom scrollbar styling (optional, for WebKit browsers) */
/* pre::-webkit-scrollbar, textarea::-webkit-scrollbar {
  width: 8px;
}
pre::-webkit-scrollbar-track, textarea::-webkit-scrollbar-track {
  background: #1f2028;
}
pre::-webkit-scrollbar-thumb, textarea::-webkit-scrollbar-thumb {
  background: #4a4a5a;
  border-radius: 4px;
}
pre::-webkit-scrollbar-thumb:hover, textarea::-webkit-scrollbar-thumb:hover {
  background: #5a5a6a;
} */

</style>
