import axios from "./axiosInstance";

// === College Course CRUD ===
export const getCollegeCourses = () => axios.get("/collegecourse");

export const getCollegeCourseById = (id) => axios.get(`/collegecourse/${id}`);

export const addCollegeCourse = (data) => axios.post("/collegecourse", data);

export const updateCollegeCourse = (id, data) =>
  axios.put(`/collegecourse/${id}`, data);

export const deleteCollegeCourse = (id) =>
  axios.delete(`/collegecourse/${id}`);

