import { useState } from "react";
import { updateSchoolYear } from "../../services/schoolYearService";
import { toast } from "react-toastify";

const EditSchoolYearModal = ({ year, onClose, onSuccess }) => {
  const [form, setForm] = useState({
    startYear: year.startYear,
    endYear: year.endYear,
  });
  const [loading, setLoading] = useState(false);

  const handleChange = (e) =>
    setForm({ ...form, [e.target.name]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);

    try {
      await updateSchoolYear(year.id, form);
      toast.success("School year updated.");
      onSuccess();
    } catch (err) {
      toast.error("Failed to update.");
      console.error("Error updating school year:", err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <dialog className="modal modal-open">
      <div className="modal-box">
        <h3 className="font-bold text-lg mb-4">Edit School Year</h3>
        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <input
              type="number"
              name="startYear"
              value={form.startYear}
              onChange={handleChange}
              className="input input-bordered w-full"
              required
            />
            <input
              type="number"
              name="endYear"
              value={form.endYear}
              onChange={handleChange}
              className="input input-bordered w-full"
              required
            />
          </div>
          <div className="modal-action">
            <button type="button" className="btn" onClick={onClose}>
              Cancel
            </button>
            <button
              type="submit"
              className={`btn btn-primary ${loading ? "loading" : ""}`}
              disabled={loading}
            >
              Update
            </button>
          </div>
        </form>
      </div>
    </dialog>
  );
};

export default EditSchoolYearModal;
