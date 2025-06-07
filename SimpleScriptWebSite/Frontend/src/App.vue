<template>
  <div class="min-h-screen flex flex-col md:flex-row bg-[#1a1b26] text-white">
    <!-- Mobile Tab Navigation (visible on mobile only) -->
    <div class="md:hidden bg-[#13141f] p-4 border-b border-gray-600">
      <h1 class="text-xl font-bold mb-4 text-center">SimpleScript</h1>
      <div class="flex space-x-2">
        <button
          id="editorTabMobile"
          ref="editorTabButtonMobile"
          class="flex-1 text-center p-3 rounded hover:bg-[#2a2b36] flex items-center justify-center gap-2"
          aria-label="Try SimpleScript"
          tabindex="0"
          @click="() => handleTabSwitch('editorTab', 'editorSection')"
          @keydown.enter.prevent="() => handleTabSwitch('editorTab', 'editorSection')"
          @keydown.space.prevent="() => handleTabSwitch('editorTab', 'editorSection')"
        >
          <span class="text-purple-400">&lt;/&gt;</span>
          <span class="hidden sm:inline">Try SimpleScript</span>
          <span class="sm:hidden">Editor</span>
        </button>
        <button
          id="aboutTabMobile"
          ref="aboutTabButtonMobile"
          class="flex-1 text-center p-3 rounded hover:bg-[#2a2b36] flex items-center justify-center gap-2"
          aria-label="About"
          tabindex="0"
          @click="() => handleTabSwitch('aboutTab', 'aboutSection')"
          @keydown.enter.prevent="() => handleTabSwitch('aboutTab', 'aboutSection')"
          @keydown.space.prevent="() => handleTabSwitch('aboutTab', 'aboutSection')"
        >
          <span class="text-gray-400">📖</span>
          About
        </button>
      </div>
    </div>

    <!-- Desktop Sidebar (hidden on mobile) -->
    <div class="hidden md:block w-64 bg-[#13141f] p-4 h-screen sticky top-0">
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
    <div class="flex-1 p-3 md:p-6 flex flex-col overflow-auto">
      <!-- Editor Section -->
      <div id="editorSection" ref="editorSectionDiv" class="flex flex-col flex-grow">
        <h2 class="text-xl md:text-2xl text-purple-400 mb-4">Code Editor</h2>

        <!-- Code Editor with Line Numbers -->
        <div
          class="relative w-full bg-[#1f2028] rounded-lg mb-4 overflow-hidden h-[50vh] md:h-[60vh]">
          <!-- Line Numbers -->
          <div
            ref="lineNumbers"
            class="absolute left-0 top-0 bg-[#161620] text-gray-500 font-mono text-xs md:text-sm leading-5 md:leading-6 p-2 md:p-4 pr-1 md:pr-2 border-r border-gray-600 select-none w-[35px] md:w-[50px] h-full overflow-hidden"
          >
            <div v-for="n in lineCount" :key="n" class="text-right">{{ n }}</div>
          </div>

          <!-- Code Textarea -->
          <textarea
            id="codeEditor"
            ref="codeTextarea"
            class="w-full h-full bg-transparent font-mono text-green-400 focus:outline-none focus:ring-2 focus:ring-purple-500 resize-none leading-5 md:leading-6 text-xs md:text-sm border-none pl-[45px] md:pl-[60px] p-2 md:p-4"
            placeholder="// Write your Simple Script code here"
            v-model="code"
            @scroll="syncScroll"
            @input="updateLineNumbers"
          ></textarea>
        </div>

        <div
          class="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-4 gap-4 sm:gap-0">
          <div class="flex items-center flex-grow w-full sm:w-auto order-2 sm:order-1">
            <input
              type="text"
              v-model="programInput"
              placeholder="Enter program input here..."
              class="flex-grow bg-[#1f2028] p-2 rounded-l-lg focus:outline-none focus:ring-2 focus:ring-purple-500 text-white text-sm"
            />
            <button
              id="sendInputButton"
              class="bg-blue-600 hover:bg-blue-700 px-3 md:px-4 py-2 rounded-r-lg flex items-center gap-2 text-sm"
              aria-label="Send Input"
              tabindex="0"
              @click="sendProgramInput"
            >
              <span>➤</span>
              <span class="hidden sm:inline">Send Input</span>
            </button>
          </div>
          <button
            :id="isRunning ? 'stopButton' : 'runButton'"
            :class="[
              'px-4 md:px-4 py-3 md:py-2 rounded-lg flex items-center justify-center gap-2 text-base md:text-sm w-full sm:w-auto sm:ml-4 order-1 sm:order-2 font-semibold shadow-lg transition-all duration-200 hover:shadow-xl',
              isRunning
                ? isStopping
                  ? 'bg-gray-600 cursor-not-allowed'
                  : 'bg-red-600 hover:bg-red-700 focus:ring-red-500'
                : 'bg-purple-600 hover:bg-purple-700 focus:ring-purple-500'
            ]"
            :disabled="isStopping"
            :aria-label="isRunning ? (isStopping ? 'Stopping...' : 'Stop Connection') : 'Run Code'"
            tabindex="0"
            @click="handleButtonClick"
            @keydown.enter.prevent="handleButtonClick"
            @keydown.space.prevent="handleButtonClick"
          >
            <span v-if="isRunning && !isStopping" class="text-lg md:text-base">⏹</span>
            <span v-else-if="!isRunning" class="text-lg md:text-base">▶</span>
            <div v-if="isStopping" class="loading-spinner-red"></div>
            <span class="font-bold">
              {{ isStopping ? 'Stopping...' : (isRunning ? 'Stop' : 'Run Code') }}
            </span>
          </button>
        </div>

        <div
          class="bg-[#1f2028] p-3 md:p-4 rounded-lg flex flex-col flex-grow min-h-0 h-[20vh] md:h-[15vh] overflow-auto">
          <h3 class="text-gray-400 mb-2 text-sm md:text-base">Output</h3>

          <!-- Loading indicators -->
          <div v-if="isCompiling" class="flex items-center gap-2 text-yellow-400 mb-2">
            <div class="loading-spinner"></div>
            <span class="text-sm">Compiling program...</span>
          </div>

          <div v-if="isStopping" class="flex items-center gap-2 text-red-400 mb-2">
            <div class="loading-spinner-red"></div>
            <span class="text-sm">Program is stopped.</span>
          </div>

          <pre id="outputArea"
               class="flex-1 overflow-auto font-mono whitespace-pre-wrap text-xs md:text-sm"
               :class="hasErrorOccurred ? 'text-red-400' : 'text-green-400'"
          >{{ output }}</pre>
        </div>
      </div>

      <!-- About Section -->
      <div id="aboutSection" ref="aboutSectionDiv" class="hidden flex-grow">
        <div class="max-w-none">
          <h2 class="text-xl md:text-2xl text-purple-400 mb-4">About SimpleScript</h2>
          <div class="space-y-4 md:space-y-6">
            <p class="text-gray-300 text-sm md:text-base">
              SimpleScript is a straightforward programming language designed for simplicity and
              ease
              of use. Perfect for learning programming concepts and building small applications.
            </p>

            <div>
              <h3 class="text-xl text-purple-400 mb-3">Language Syntax & Features:</h3>

              <div class="space-y-4">
                <!-- Variables -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Variables</h4>
                  <p class="text-gray-300 mb-2">Declare variables using the <code
                    class="text-yellow-400">LET</code> keyword. The types of the variables are
                    automatically inferred:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">
LET a = 0
LET name = "John"
LET is_true = TRUE</pre>
                </div>

                <!-- Input/Output -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Input & Output</h4>
                  <p class="text-gray-300 mb-2">Print to console and read user input:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">PRINT "Hello World!"
PRINT "\n"  // New line
INPUT variable_name  // Read input into variable</pre>
                </div>

                <!-- Conditionals -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Conditionals</h4>
                  <p class="text-gray-300 mb-2">Control flow with if statements:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">
IF nums < 0 DO
    PRINT "Number should be greater or equal to zero!"
ENDIF</pre>
                </div>

                <!-- Loops -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Loops</h4>
                  <p class="text-gray-300 mb-2">Iterate with while loops:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">
WHILE nums > 0 REPEAT
    PRINT a
    LET nums = nums - 1
ENDWHILE</pre>
                </div>

                <!-- Functions -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Functions</h4>
                  <p class="text-gray-300 mb-2">Define reusable functions with parameters:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">
FUNC add(int num_1, int num_2)
BODY
    LET result = num_1 + num_2
    RETURN result
ENDBODY

PRINT add(10, 20)
</pre>
                </div>

                <!-- Built-in Functions -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Built-in Functions</h4>
                  <p class="text-gray-300 mb-2">Utility functions for common operations:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">ToNumber(string)  // Convert string to number</pre>
                </div>

                <!-- Data Types -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Data Types</h4>
                  <ul class="text-gray-300 space-y-1">
                    <li>• <code class="text-yellow-400">int</code> - Integer numbers</li>
                    <li>• <code class="text-yellow-400">string</code> - Text (enclosed in quotes)
                    </li>
                    <li>• <code class="text-yellow-400">bool</code> - Boolean values: <code
                      class="text-yellow-400">TRUE</code> or <code
                      class="text-yellow-400">FALSE</code></li>
                    <li>• Variables are automatically type-inferred from their assigned values</li>
                  </ul>
                </div>

                <!-- Operators -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Operators</h4>
                  <div class="space-y-3">
                    <div>
                      <h5 class="text-yellow-400 mb-1">Arithmetic Operators:</h5>
                      <ul class="text-gray-300 space-y-1 ml-4">
                        <li>• <code class="text-yellow-400">+</code> - Addition</li>
                        <li>• <code class="text-yellow-400">-</code> - Subtraction</li>
                        <li>• <code class="text-yellow-400">*</code> - Multiplication</li>
                        <li>• <code class="text-yellow-400">/</code> - Division</li>
                        <li>• <code class="text-yellow-400">**</code> - Power/Exponentiation</li>
                      </ul>
                    </div>
                    <div>
                      <h5 class="text-yellow-400 mb-1">Comparison Operators:</h5>
                      <ul class="text-gray-300 space-y-1 ml-4">
                        <li>• <code class="text-yellow-400">==</code> - Equal to</li>
                        <li>• <code class="text-yellow-400">!=</code> - Not equal to</li>
                        <li>• <code class="text-yellow-400">&lt;</code> - Less than</li>
                        <li>• <code class="text-yellow-400">&gt;</code> - Greater than</li>
                        <li>• <code class="text-yellow-400">&lt;=</code> - Less than or equal</li>
                        <li>• <code class="text-yellow-400">&gt;=</code> - Greater than or equal
                        </li>
                      </ul>
                    </div>
                    <div>
                      <h5 class="text-yellow-400 mb-1">Assignment:</h5>
                      <ul class="text-gray-300 space-y-1 ml-4">
                        <li>• <code class="text-yellow-400">=</code> - Assignment operator</li>
                      </ul>
                    </div>
                  </div>
                </div>

                <!-- Boolean Values -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Boolean Values</h4>
                  <p class="text-gray-300 mb-2">SimpleScript supports boolean logic with predefined
                    constants:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">
LET is_valid = TRUE
LET is_false = FALSE

IF is_valid == TRUE DO
    PRINT "Condition is true!"
ENDIF</pre>
                </div>

                <!-- Function Parameters -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Function Parameter Types</h4>
                  <p class="text-gray-300 mb-2">Functions can accept multiple typed parameters:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">
FUNC calculate(int x, int y, bool should_add)
BODY
    IF should_add == TRUE DO
        RETURN x + y
    ENDIF
    RETURN x - y
ENDBODY

FUNC greet(string name, int age)
BODY
    PRINT "Hello "
    PRINT name
    PRINT ", you are "
    PRINT age
    PRINT " years old"
ENDBODY</pre>
                </div>

                <!-- Expression Examples -->
                <div class="bg-[#2a2b36] p-4 rounded-lg">
                  <h4 class="text-lg text-blue-400 mb-2">Expression Examples</h4>
                  <p class="text-gray-300 mb-2">Complex expressions with parentheses and multiple
                    operators:</p>
                  <pre class="text-green-400 bg-[#1f2028] p-2 rounded text-sm">
LET result = (10 + 5) * 2
LET power = 2 ** 3  // 8
LET complex = (x + y) / (a - b)

IF (age >= 18) AND (score > 80) DO
    PRINT "Eligible!"
ENDIF</pre>
                </div>
              </div>
            </div>

            <div class="bg-[#2a2b36] p-4 rounded-lg text-gray-300">
              <h4 class="text-lg text-blue-400 mb-2">Getting Started</h4>
              <p>To get started, switch to the "Try SimpleScript" tab, write your code in the
                editor,
                and click "Run" to see the output. The language uses clear, English-like keywords
                making it easy to read and understand. All operators follow standard mathematical
                precedence rules, and parentheses can be used to control evaluation order.</p>
            </div>
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
const editorTabButtonMobile = ref<HTMLButtonElement | null>(null);
const aboutTabButtonMobile = ref<HTMLButtonElement | null>(null);
const editorSectionDiv = ref<HTMLDivElement | null>(null);
const aboutSectionDiv = ref<HTMLDivElement | null>(null);
const lineNumbers = ref<HTMLDivElement | null>(null);
const codeTextarea = ref<HTMLTextAreaElement | null>(null);

// Reactive state
const code = ref('');
const output = ref('');
const programInput = ref('');
const socket = ref<WebSocket | null>(null);
const isRunning = ref(false);
const hasErrorOccurred = ref(false);
const isCompiling = ref(false);
const isStopping = ref(false);
const lineCount = ref(1);

onMounted(() => {
  handleTabSwitch('editorTab', 'editorSection');
  code.value = exampleProgram;
  updateLineNumbers();
});

onUnmounted(() => {
  if (socket.value) {
    socket.value.close();
  }
});

const handleTabSwitch = (activeTabId: TabId, activeSectionId: SectionId) => {
  // Remove active state from all tabs (desktop and mobile)
  [editorTabButton.value, aboutTabButton.value, editorTabButtonMobile.value, aboutTabButtonMobile.value].forEach(tab => {
    tab?.classList.remove('bg-[#2a2b36]');
  });

  // Hide all sections
  [editorSectionDiv.value, aboutSectionDiv.value].forEach(section => {
    section?.classList.add('hidden');
  });

  // Activate the selected tab and section
  if (activeTabId === 'editorTab') {
    editorTabButton.value?.classList.add('bg-[#2a2b36]');
    editorTabButtonMobile.value?.classList.add('bg-[#2a2b36]');
    editorSectionDiv.value?.classList.remove('hidden');
  } else if (activeTabId === 'aboutTab') {
    aboutTabButton.value?.classList.add('bg-[#2a2b36]');
    aboutTabButtonMobile.value?.classList.add('bg-[#2a2b36]');
    aboutSectionDiv.value?.classList.remove('hidden');
  }
};

const handleButtonClick = () => {
  if (isRunning.value) {
    isStopping.value = true;
    isCompiling.value = false;
    stopWebsocket();
  } else {
    handleRunCode();
    // Scroll to bottom to show output area
    setTimeout(() => {
      window.scrollTo({
        top: document.body.scrollHeight,
        behavior: 'smooth'
      });
    }, 100);
  }
}

const handleRunCode = () => {
  isRunning.value = true;
  output.value = "";
  hasErrorOccurred.value = false;
  isCompiling.value = true;

  const protocol = window.location.protocol === 'https:' ? 'wss' : 'ws';
  let wsUrl: string;

  if (import.meta.env.PROD) {
    wsUrl = `${protocol}://${window.location.host}/simple-script/ws`;
  } else {
    wsUrl = `${protocol}://localhost:40090/ws`;
  }

  try {
    socket.value = new WebSocket(wsUrl);

    socket.value.onopen = () => {
      isCompiling.value = true;
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
      // Hide loading indicator on first output
      if (isCompiling.value) {
        isCompiling.value = false;
      }

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
      isCompiling.value = false;
      isStopping.value = false;
    };

    socket.value.onclose = () => {
      console.log("Websocket closed");
      socket.value = null;
      isRunning.value = false;
      isCompiling.value = false;
      isStopping.value = false;
    };
  } catch (error) {
    console.error("Failed to create WebSocket:", error);
    output.value += `Failed to initialize WebSocket connection: ${error}. Please ensure the server is running and the URL is correct.\n`;
    socket.value = null;
    isRunning.value = false;
    hasErrorOccurred.value = true;
    isCompiling.value = false;
    isStopping.value = false;
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
    isStopping.value = false;
  }
};

const updateLineNumbers = () => {
  const lines = code.value.split('\n').length;
  lineCount.value = Math.max(1, lines);
};

const syncScroll = () => {
  if (lineNumbers.value && codeTextarea.value) {
    lineNumbers.value.scrollTop = codeTextarea.value.scrollTop;
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

/* Loading spinner animation */
.loading-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid #374151;
  border-top: 2px solid #fbbf24;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

/* Red loading spinner for stopping state */
.loading-spinner-red {
  width: 16px;
  height: 16px;
  border: 2px solid #374151;
  border-top: 2px solid #ef4444;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
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
