<template>
  <div class="chat-container">
    <h2>Ollama C# Chat</h2>
    <div class="messages" ref="chatBox">
      <div v-for="(msg, index) in messages" :key="index" :class="['message-item', msg.role]">
        <div class="role-tag">{{ msg.role === 'user' ? '你' : 'AI' }}</div>
        <div class="content">{{ msg.content }}</div>
      </div>
    </div>
    
    <div class="input-area">
      <input 
        v-model="userInput" 
        @keyup.enter="sendMessage" 
        placeholder="输入问题并按回车..." 
        :disabled="isLoading"
      />
      <button @click="sendMessage" :disabled="isLoading">
        {{ isLoading ? '思考中...' : '发送' }}
      </button>
      <button @click="resetChat" class="reset-btn">清空记忆</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, nextTick } from 'vue';

// 消息接口定义
interface Message {
  role: 'user' | 'assistant';
  content: string;
}

const userInput = ref('');
const messages = ref<Message[]>([]);
const context = ref<number[] | null>(null); // 存储对话上下文
const isLoading = ref(false);
const chatBox = ref<HTMLElement | null>(null);

// 重置对话
const resetChat = () => {
  messages.value = [];
  context.value = null;
};

// 自动滚动到底部
const scrollToBottom = async () => {
  await nextTick();
  if (chatBox.value) {
    chatBox.value.scrollTop = chatBox.value.scrollHeight;
  }
};

const sendMessage = async () => {
  if (!userInput.value || isLoading.value) return;

  const prompt = userInput.value;
  messages.value.push({ role: 'user', content: prompt });
  userInput.value = '';
  isLoading.value = true;

  // 创建 AI 回答的响应式引用
  const aiMessage: Message = { role: 'assistant', content: '' };
  messages.value.push(aiMessage);
  await scrollToBottom();

  try {
    const response = await fetch('http://localhost:5062/api/Chat/chat-stream', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ 
        prompt: prompt, 
        context: context.value 
      })
    });

    if (!response.body) throw new Error("无法读取流");

    // App.vue 核心解析部分
const reader = response.body.getReader();
const decoder = new TextDecoder("utf-8");
let buffer = ""; // 缓冲区

while (true) {
  const { done, value } = await reader.read();
  if (done) break;

  // 1. 解码并放入缓冲区
  buffer += decoder.decode(value, { stream: true });

  // 2. 检查是否有换行符（后端在 JSON 前加了 \n）
  if (buffer.includes('\n')) {
    const lines = buffer.split('\n');
    
    // 除了最后一行（可能不完整），循环处理所有行
    for (let i = 0; i < lines.length - 1; i++) {
      const line = lines[i]?.trim();
      if (!line) continue; // 如果 line 是 undefined 或空字符串，跳过

      // 如果这一行是 JSON 记忆
      if (line.startsWith('{"context"')) {
        try {
          const data = JSON.parse(line);
          context.value = data.context; 
          console.log("✅ 记忆已存入 context.value:", context.value);
        } catch (e) {
          console.error("❌ 解析 JSON 失败:", e);
        }
      } else {
        // 如果是正常的聊天文字
        aiMessage.content += line;
      }
    }
    // 把最后一行留给下次循环，因为它可能还没接收完
    buffer = lines[lines.length - 1]?? "";
  } else {
    // 如果没有换行符，且看起来不是 JSON 的开始，直接显示文字
    // 这样做是为了让打字机效果更丝滑
    if (!buffer.startsWith('{')) {
      aiMessage.content += buffer;
      buffer = "";
    }
  }
  await scrollToBottom();
}

// 循环结束后，处理 buffer 里剩下的最后一点点数据
if (buffer.trim()) {
  const finalLine = buffer.trim();
  if (finalLine.startsWith('{"context"')) {
    try {
      const data = JSON.parse(finalLine);
      context.value = data.context;
      console.log("✅ 最终记忆已更新");
    } catch {}
  } else {
    aiMessage.content += finalLine;
  }
}


// 结束后，处理 buffer 剩下的最后一点内容（可能是最后的 JSON 或文字）
if (buffer.trim()) {
  const lastLine = buffer.trim();
  if (lastLine.startsWith('{"context"')) {
    try {
      const data = JSON.parse(lastLine);
      context.value = data.context;
    } catch {}
  } else {
    aiMessage.content += lastLine;
  }
}
  } catch (error) {
    console.error("通信失败:", error);
    aiMessage.content += "\n[错误: 无法连接到后端服务器]";
  } finally {
    isLoading.value = false;
    await scrollToBottom();
  }
};
</script>

<style scoped>
.chat-container {
  max-width: 900px;
  margin: 2rem auto;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  display: flex;
  flex-direction: column;
  height: 85vh;
  background: #f9f9f9;
  border-radius: 12px;
  box-shadow: 0 4px 20px rgba(0,0,0,0.1);
  padding: 20px;
}

.messages {
  flex: 1;
  overflow-y: auto;
  padding: 1rem;
  background: white;
  border-radius: 8px;
  border: 1px solid #eee;
}

.message-item {
  margin-bottom: 1.5rem;
  display: flex;
  flex-direction: column;
}

.role-tag {
  font-weight: bold;
  font-size: 0.8rem;
  margin-bottom: 4px;
  text-transform: uppercase;
}

.user .role-tag { color: #0078d4; }
.assistant .role-tag { color: #28a745; }

.content {
  padding: 12px 16px;
  border-radius: 8px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-wrap: break-word;
}

.user .content { background: #e1f5fe; align-self: flex-start; }
.assistant .content { background: #f1f8e9; align-self: flex-start; }

.input-area {
  display: flex;
  gap: 10px;
  margin-top: 20px;
}

input {
  flex: 1;
  padding: 12px;
  border: 2px solid #ddd;
  border-radius: 6px;
  outline: none;
}

input:focus { border-color: #0078d4; }

button {
  padding: 0 20px;
  background: #0078d4;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
}

button:disabled { background: #ccc; }

.reset-btn { background: #666; }
.reset-btn:hover { background: #444; }
</style>