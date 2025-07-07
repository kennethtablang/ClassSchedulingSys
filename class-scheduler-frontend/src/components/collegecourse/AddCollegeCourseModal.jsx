import { useState } from "react";
import { addCollegeCourse } from "../../services/collegeCourseService";
import { toast } from "react-toastify";

const AddCollegeCourseModal = ({ onSuccess }) => {
  const [show, setShow] = useState(false);
  const [form, setForm] = useState({ code: "", name: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const toggleModal = () => {
    setShow(!show);
    setForm({ code: "", name: "" });
    setError("");
  };

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
      await addCollegeCourse(form);
      toast.success("Course added.");
      toggleModal();
      onSuccess();
    } catch (err) {
      setError(err?.response?.data || "Failed to add course.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button className="btn btn-primary" onClick={toggleModal}>
        + Add Course
      </button>

      {show && (
        <dialog className="modal modal-open">
          <div className="modal-box max-w-md">
            <h3 className="text-lg font-bold mb-4">Add College Course</h3>
            {error && (
              <div className="alert alert-error text-sm mb-3">{error}</div>
            )}
            <form onSubmit={handleSubmit} className="space-y-4">
              <input
                type="text"
                name="code"
                placeholder="Course Code (e.g. BSIT)"
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
                <button type="button" className="btn" onClick={toggleModal}>
                  Cancel
                </button>
                <button
                  type="submit"
                  className={`btn btn-primary ${loading ? "loading" : ""}`}
                  disabled={loading}
                >
                  Save
                </button>
              </div>
            </form>
          </div>
        </dialog>
      )}
    </>
  );
};

export default AddCollegeCourseModal;
