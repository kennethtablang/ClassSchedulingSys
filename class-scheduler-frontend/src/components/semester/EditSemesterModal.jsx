// src/components/semester/EditSemesterModal.jsx
import { useEffect, useState } from "react";
import { updateSemester } from "../../services/semesterService";
import { getSchoolYears } from "../../services/schoolYearService";
import { toast } from "react-toastify";

const EditSemesterModal = ({ semester, onClose, onSuccess }) => {
  const [form, setForm] = useState({ ...semester });
  const [schoolYears, setSchoolYears] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleChange = (e) => {
    setForm((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  useEffect(() => {
    const fetchYears = async () => {
      try {
        const { data } = await getSchoolYears();
        setSchoolYears(data);
      } catch {
        toast.error("Failed to load school years.");
      }
    };
    fetchYears();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await updateSemester(semester.id, form);
      toast.success("Semester updated.");
      onSuccess();
    } catch (err) {
      setError(err?.response?.data || "Failed to update semester.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <dialog className="modal modal-open">
      <div className="modal-box max-w-xl">
        <h3 className="text-lg font-bold mb-4">Edit Semester</h3>
        {error && <div className="alert alert-error text-sm mb-3">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <input
            type="text"
            name="name"
            value={form.name}
            onChange={handleChange}
            className="input input-bordered w-full"
            required
          />
          <div className="grid grid-cols-2 gap-4">
            <input
              type="date"
              name="startDate"
              value={form.startDate?.split("T")[0] || ""}
              onChange={handleChange}
              className="input input-bordered w-full"
              required
            />
            <input
              type="date"
              name="endDate"
              value={form.endDate?.split("T")[0] || ""}
              onChange={handleChange}
              className="input input-bordered w-full"
              required
            />
          </div>
          <select
            name="schoolYearId"
            value={form.schoolYearId}
            onChange={handleChange}
            className="select select-bordered w-full"
            required
          >
            <option value="">Select School Year</option>
            {schoolYears.map((sy) => (
              <option key={sy.id} value={sy.id}>
                {sy.label}
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

export default EditSemesterModal;
