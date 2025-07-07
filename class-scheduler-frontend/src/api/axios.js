import axios from "axios";

// Create instance
const api = axios.create({
  baseURL: "https://localhost:7037/api", // Change this for deployment (e.g. environment variable)
  headers: {
    "Content-Type": "application/json",
  },
});

// Automatically attach token to all requests
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem("token");

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
      console.log("[Axios] Attached Token:", token); // 🔍 Debug line (remove in production)
    } else {
      console.warn("[Axios] No token found.");
    }

    return config;
  },
  (error) => {
    console.error("[Axios] Request error:", error);
    return Promise.reject(error);
  }
);

// Optional: Response error interceptor
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      console.warn("[Axios] Unauthorized - maybe token expired");
      // Optional: redirect to login
      // window.location.href = "/login";
    }

    return Promise.reject(error);
  }
);

export default api;
