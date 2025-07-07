import { useState } from "react";
import { updateCollegeCourse } from "../../services/collegeCourseService";
import { toast } from "react-toastify";

const EditCollegeCourseModal = ({ course, onClose, onSuccess }) => {
  const [form, setForm] = useState({
    id: course.id,
    code: course.code,
    name: course.name,
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleChange = (e) => {
    setForm((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    try {
      await updateCollegeCourse(form.id, form);
      toast.success("Course updated.");
      onSuccess();
    } catch (err) {
      setError(err?.response?.data || "Failed to update course.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <dialog className="modal modal-open">
      <div className="modal-box max-w-md">
        <h3 className="text-lg font-bold mb-4">Edit Course</h3>
        {error && <div className="alert alert-error text-sm mb-3">{error}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <input
            type="text"
            name="code"
            placeholder="Course Code"
            value={form.code}
            onChange={handleChange}
            className="input input-bordered w-full"
            required
          />
          <input
            type="text"
            name="name"
            placeholder="Course Name"
            value={form.name}
            onChange={handleChange}
            className="input input-bordered w-full"
            required
          />

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

export default EditCollegeCourseModal;
