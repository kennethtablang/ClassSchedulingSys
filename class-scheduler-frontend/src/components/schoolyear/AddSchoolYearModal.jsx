import { useState } from "react";
import { addSchoolYear } from "../../services/schoolYearService";
import { toast } from "react-toastify";

const AddSchoolYearModal = ({ onSuccess }) => {
  const [show, setShow] = useState(false);
  const [form, setForm] = useState({ startYear: "", endYear: "" });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const toggleModal = () => {
    setShow(!show);
    setError("");
  };

  const handleChange = (e) => {
    setForm({ ...form, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError("");

    try {
      await addSchoolYear(form);
      toast.success("School year added!");
      toggleModal();
      setForm({ startYear: "", endYear: "" });
      onSuccess();
    } catch (err) {
      setError(err?.response?.data || "Failed to add school year.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button className="btn btn-primary" onClick={toggleModal}>
        + Add School Year
      </button>

      {show && (
        <dialog className="modal modal-open">
          <div className="modal-box max-w-md">
            <h3 className="text-lg font-bold mb-4">Add School Year</h3>
            {error && <div className="alert alert-error text-sm">{error}</div>}
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <input
                  type="number"
                  name="startYear"
                  placeholder="Start Year"
                  className="input input-bordered w-full"
                  value={form.startYear}
                  onChange={handleChange}
                  required
                />
                <input
                  type="number"
                  name="endYear"
                  placeholder="End Year"
                  className="input input-bordered w-full"
                  value={form.endYear}
                  onChange={handleChange}
                  required
                />
              </div>
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

export default AddSchoolYearModal;
