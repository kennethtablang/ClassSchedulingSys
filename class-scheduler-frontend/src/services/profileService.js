// src/services/profileService.js
import axios from "./axiosInstance"; // ✅ use your configured instance

export const getProfile = async () => {
  const res = await axios.get("/auth/profile");
  return res.data;
};

export const updateProfile = async (profileData) => {
  const res = await axios.put("/auth/profile", profileData);
  return res.data;
};
