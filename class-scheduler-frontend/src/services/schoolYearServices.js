// src/services/schoolYearService.js
import axios from "./axiosInstance";

export const getSchoolYears    = () => axios.get("/schoolyear");
export const getSchoolYearById = (id) => axios.get(`/schoolyear/${id}`);
export const addSchoolYear     = (data) => axios.post("/schoolyear", data);
export const updateSchoolYear  = (id, data) => axios.put(`/schoolyear/${id}`, data);
export const deleteSchoolYear  = (id) => axios.delete(`/schoolyear/${id}`);
