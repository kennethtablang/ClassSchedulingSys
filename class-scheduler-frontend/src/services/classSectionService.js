// src/services/classSectionService.js
import axiosInstance from "./axiosInstance";

export const getClassSections = () => axiosInstance.get("/classsection");

export const getClassSectionById = (id) =>
  axiosInstance.get(`/classsection/${id}`);

export const addClassSection = (data) =>
  axiosInstance.post("/classsection", data);

export const updateClassSection = (id, data) =>
  axiosInstance.put(`/classsection/${id}`, data);

export const deleteClassSection = (id) =>
  axiosInstance.delete(`/classsection/${id}`);

export const createClassSection = (data) =>
  axiosInstance.post("/classsection", data);