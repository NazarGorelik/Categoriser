<template>
  <main>
    <section class="card">
      <h1>ISIN Kategoriser</h1>
      <p>Lade eine Excel-Datei hoch, prüfe die ISINs und lade die Kategorien herunter.</p>
      <input type="file" accept=".xlsx,.xls" @change="onFileChange" />
      <div style="margin-top: 1rem;">
        <button :disabled="!selectedFile || loading" @click="upload">Upload</button>
        <button :disabled="!jobId || loading" style="margin-left: 0.5rem;" @click="check">ISINs prüfen</button>
      </div>
      <p v-if="jobId" style="margin-top: 0.5rem;">Job-ID: <span class="badge">{{ jobId }}</span></p>
      <p v-if="errors.length" style="color: #b91c1c;">{{ errors.join(" | ") }}</p>
    </section>

    <section v-if="summary" class="card">
      <h2>Zusammenfassung</h2>
      <ul>
        <li v-for="(count, key) in summary" :key="key">
          <strong>{{ key }}</strong>: {{ count }}
        </li>
      </ul>
      <div style="margin-top: 1rem;">
        <button @click="download('singleSheet')">Gesamt (1 Sheet)</button>
        <button style="margin-left: 0.5rem;" @click="download('sheetsByCategory')">Pro Kategorie (Sheets)</button>
      </div>
    </section>

    <section v-if="rows.length" class="card">
      <h2>Ergebnisse</h2>
      <table>
        <thead>
          <tr>
            <th>ISIN</th>
            <th>Name</th>
            <th>WKN</th>
            <th>Kategorie</th>
            <th>Unterkategorie</th>
            <th>Position</th>
            <th>Fehler</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in rows" :key="row.isin">
            <td>{{ row.isin }}</td>
            <td>{{ row.name }}</td>
            <td>{{ row.wkn }}</td>
            <td>{{ row.category }}</td>
            <td>{{ row.subCategory }}</td>
            <td>{{ row.positionType }}</td>
            <td style="color: #b91c1c;">{{ row.error }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  </main>
</template>

<script setup lang="ts">
import { ref } from "vue";

type UploadRow = { isin: string; name: string; wkn: string };

type CategorizedRow = {
  isin: string;
  name: string;
  wkn: string;
  category: string;
  subCategory: string;
  positionType: string;
  error: string;
};

const selectedFile = ref<File | null>(null);
const jobId = ref<string | null>(null);
const rows = ref<CategorizedRow[]>([]);
const summary = ref<Record<string, number> | null>(null);
const errors = ref<string[]>([]);
const loading = ref(false);

const onFileChange = (event: Event) => {
  const target = event.target as HTMLInputElement;
  selectedFile.value = target.files?.[0] ?? null;
};

const upload = async () => {
  if (!selectedFile.value) return;
  loading.value = true;
  errors.value = [];
  summary.value = null;

  const formData = new FormData();
  formData.append("file", selectedFile.value);

  const response = await fetch("/api/upload", {
    method: "POST",
    body: formData
  });

  if (!response.ok) {
    errors.value = ["Upload fehlgeschlagen."];
    loading.value = false;
    return;
  }

  const data = (await response.json()) as { jobId: string; rows: UploadRow[]; errors: string[] };
  jobId.value = data.jobId;
  rows.value = data.rows.map((row) => ({
    ...row,
    category: "",
    subCategory: "",
    positionType: "",
    error: ""
  }));
  errors.value = data.errors ?? [];
  loading.value = false;
};

const check = async () => {
  if (!jobId.value) return;
  loading.value = true;

  const response = await fetch("/api/check", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ jobId: jobId.value })
  });

  if (!response.ok) {
    errors.value = ["Prüfung fehlgeschlagen."];
    loading.value = false;
    return;
  }

  const data = (await response.json()) as { summary: Record<string, number>; rows: CategorizedRow[]; errors: string[] };
  summary.value = data.summary;
  rows.value = data.rows;
  errors.value = data.errors ?? [];
  loading.value = false;
};

const download = (mode: "singleSheet" | "sheetsByCategory") => {
  if (!jobId.value) return;
  window.location.href = `/api/download?jobId=${jobId.value}&mode=${mode}`;
};
</script>
