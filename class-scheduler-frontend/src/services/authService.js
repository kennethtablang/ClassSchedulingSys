import api from "../api/axios";

// Login user with credentials
export const loginUser = async (credentials) => {
  const response = await api.post("/auth/login", credentials);
  const { token } = response.data;

    // Store JWT token
    if (token) {
        localStorage.setItem("token", token); // ✅ store it for use in requests
        console.log("[AuthService] Token stored:", token); // 🔍 Debug line (remove in production)
    }

  return response.data;
};

// Register new user
export const registerUser = async (userData) => {
  const response = await api.post("/auth/register", userData);
  return response.data;
};

// Optional: Logout
export const logoutUser = () => {
  localStorage.removeItem("token");
};