// src/components/classsection/EditClassSectionModal.jsx
import { useEffect, useState } from "react";
import { getCollegeCourses } from "../../services/collegeCourseService";
import { getSemesters } from "../../services/semesterService";
import { updateClassSection } from "../../services/classSectionService";
import { toast } from "react-toastify";

const EditClassSectionModal = ({ section, onClose, onSuccess }) => {
  const [form, setForm] = useState({ ...section });
  const [courses, setCourses] = useState([]);
  const [semesters, setSemesters] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    const fetchData = async () => {
      try {
        const courseRes = await getCollegeCourses();
        setCourses(courseRes.data);
        const semesterRes = await getSemesters();
        setSemesters(semesterRes.data);
      } catch {
        toast.error("Failed to load course or semester data.");
      }
    };
    fetchData();
  }, []);

  const handleChange = (e) => {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await updateClassSection(form.id, form);
      toast.success("Class section updated successfully.");
      onSuccess();
    } catch (err) {
      setError(err?.response?.data || "Failed to update section.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <dialog className="modal modal-open">
      <div className="modal-box max-w-xl">
        <h3 className="text-lg font-bold mb-4">Edit Class Section</h3>
        {error && <div className="alert alert-error mb-3 text-sm">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <input
            type="text"
            name="section"
            value={form.section}
            onChange={handleChange}
            placeholder="Section (e.g., A, B)"
            className="input input-bordered w-full"
            required
          />

          <select
            name="yearLevel"
            className="select select-bordered w-full"
            value={form.yearLevel}
            onChange={handleChange}
            required
          >
            <option value="">Select Year Level</option>
            {[1, 2, 3, 4].map((y) => (
              <option key={y} value={y}>{`${y}st Year`}</option>
            ))}
          </select>

          <select
            name="collegeCourseId"
            className="select select-bordered w-full"
            value={form.collegeCourseId}
            onChange={handleChange}
            required
          >
            <option value="">Select Course</option>
            {courses.map((course) => (
              <option key={course.id} value={course.id}>
                {course.code} - {course.name}
              </option>
            ))}
          </select>

          <select
            name="semesterId"
            className="select select-bordered w-full"
            value={form.semesterId}
            onChange={handleChange}
            required
          >
            <option value="">Select Semester</option>
            {semesters.map((sem) => (
              <option key={sem.id} value={sem.id}>
                {sem.name} ({sem.schoolYearLabel})
              </option>
            ))}
          </select>

          <div className="modal-action">
            <button type="button" className="btn" onClick={onClose}>
              Cancel
            </button>
            <button
              type="submit"
              className={`btn btn-primary ${loading ? "loading" : ""}`}
              disabled={loading}
            >
              Save Changes
            </button>
          </div>
        </form>
      </div>
    </dialog>
  );
};

export default EditClassSectionModal;
