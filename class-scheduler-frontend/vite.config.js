import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// No need to import tailwindcss here!
export default defineConfig({
  plugins: [react()],
});
