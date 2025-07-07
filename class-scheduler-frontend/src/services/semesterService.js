import axios from "./axiosInstance";

// === SEMESTER CRUD ===

// Get all semesters
export const getSemesters = () => axios.get("/semester");

// Add a new semester
export const addSemester = (data) => axios.post("/semester", data);

// Update existing semester
export const updateSemester = (id, data) =>
  axios.put(`/semester/${id}`, data);

// Delete semester
export const deleteSemester = (id) => axios.delete(`/semester/${id}`);

export const setSemesterAsCurrent = (id) =>
  axios.patch(`/semester/${id}/set-current`);
