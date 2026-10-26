<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref, watch } from "vue";
import JSONEditor from "jsoneditor";
import "jsoneditor/dist/jsoneditor.min.css";

const props = defineProps<{
  modelValue: string;
}>();

const emit = defineEmits<{
  "update:modelValue": [value: string];
}>();

const editorContainer = ref<HTMLElement | null>(null);
let editor: JSONEditor | null = null;

const formatJson = () => {
  if (!editor) return;

  try {
    const text = (editor as typeof editor & { getText?: () => string }).getText?.() ?? "{}";
    const parsed = JSON.parse(text);
    const formatted = JSON.stringify(parsed, null, 2);
    (editor as typeof editor & { setText?: (value: string) => void }).setText?.call(editor, formatted);
    emit("update:modelValue", formatted);
  } catch {
    // ignore invalid JSON when formatting shortcut is pressed
  }
};

const applyEditorValue = (value: string) => {
  if (!editor) return;

  const nextValue = value || "{}";
  const setText = (editor as typeof editor & { setText?: (value: string) => void }).setText;

  if (typeof setText === "function") {
    setText.call(editor, nextValue);
    return;
  }

  try {
    editor.set(JSON.parse(nextValue));
  } catch {
    setText?.call(editor, nextValue);
  }
};

onMounted(() => {
  if (!editorContainer.value) return;

  editor = new JSONEditor(editorContainer.value, {
    mode: "code",
    mainMenuBar: true,
    navigationBar: true,
    statusBar: true,
    indentation: 2,
    sortKeys: false,
    escapeUnicode: false,
    theme: "default",
    onChangeText: value => emit("update:modelValue", value)
  });

  const handleShortcut = (event: KeyboardEvent) => {
    if (event.altKey && event.shiftKey && event.key.toLowerCase() === "f") {
      event.preventDefault();
      formatJson();
    }
  };

  window.addEventListener("keydown", handleShortcut);
  (editor as typeof editor & { __formatShortcut?: (event: KeyboardEvent) => void }).__formatShortcut = handleShortcut;

  applyEditorValue(props.modelValue);
});

watch(
  () => props.modelValue,
  value => {
    if (!editor) return;

    const current = (editor as typeof editor & { getText?: () => string }).getText?.() ?? "";
    if (current !== value) {
      applyEditorValue(value);
    }
  }
);

onBeforeUnmount(() => {
  const shortcut = (editor as typeof editor & { __formatShortcut?: (event: KeyboardEvent) => void }).__formatShortcut;
  if (shortcut) {
    window.removeEventListener("keydown", shortcut);
  }
  editor?.destroy();
  editor = null;
});
</script>

<template>
  <div ref="editorContainer" class="json-editor" />
</template>

<style scoped>
.json-editor {
  width: 100%;
  min-height: 360px;
  height: 460px;
}

:deep(.jsoneditor) {
  width: 100%;
  height: 100%;
  border-radius: 8px;
  background: #ffffff;
  color: #1f2937;
  border: 1px solid #e5e7eb;
}

:deep(.jsoneditor-menu) {
  background: #111827;
  border-bottom: 1px solid #374151;
  color: #f9fafb;
}

:deep(.jsoneditor-contextmenu) {
  background: #ffffff;
  border: 1px solid #e5e7eb;
  color: #1f2937;
}

:deep(.jsoneditor textarea),
:deep(.jsoneditor .ace_editor) {
  background: #ffffff !important;
  color: #111827 !important;
}

:deep(.jsoneditor .ace_gutter) {
  background: #f8fafc !important;
  color: #64748b !important;
}

:deep(.jsoneditor .ace_text-layer) {
  color: #111827 !important;
}
</style>
