// src/services/axiosInstance.js
import axios from "axios";

const API = "https://localhost:7037/api"; // Or use env: import.meta.env.VITE_API_URL

const axiosInstance = axios.create({
  baseURL: API,
});

axiosInstance.interceptors.request.use((config) => {
  const token = localStorage.getItem("token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default axiosInstance; // ← THIS is what's missing in your file
