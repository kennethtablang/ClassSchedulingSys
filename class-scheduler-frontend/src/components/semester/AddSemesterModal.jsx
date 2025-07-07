// src/components/semester/AddSemesterModal.jsx
import { useEffect, useState } from "react";
import { addSemester } from "../../services/semesterService";
import { getSchoolYears } from "../../services/schoolYearService";
import { toast } from "react-toastify";

const AddSemesterModal = ({ onSuccess }) => {
  const [show, setShow] = useState(false);
  const [schoolYears, setSchoolYears] = useState([]);
  const [form, setForm] = useState({
    name: "",
    startDate: "",
    endDate: "",
    schoolYearId: "",
  });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const toggleModal = () => {
    setShow(!show);
    setError("");
  };

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
      await addSemester(form);
      toast.success("Semester added.");
      toggleModal();
      onSuccess();
      setForm({ name: "", startDate: "", endDate: "", schoolYearId: "" });
    } catch (err) {
      setError(err?.response?.data || "Failed to add semester.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <button className="btn btn-primary" onClick={toggleModal}>
        + Add Semester
      </button>

      {show && (
        <dialog className="modal modal-open">
          <div className="modal-box max-w-xl">
            <h3 className="text-lg font-bold mb-4">Add Semester</h3>
            {error && (
              <div className="alert alert-error text-sm mb-3">{error}</div>
            )}
            <form onSubmit={handleSubmit} className="space-y-4">
              <input
                type="text"
                name="name"
                placeholder="Semester Name (e.g. 1st Sem)"
                value={form.name}
                onChange={handleChange}
                className="input input-bordered w-full"
                required
              />
              <div className="grid grid-cols-2 gap-4">
                <input
                  type="date"
                  name="startDate"
                  value={form.startDate}
                  onChange={handleChange}
                  className="input input-bordered w-full"
                  required
                />
                <input
                  type="date"
                  name="endDate"
                  value={form.endDate}
                  onChange={handleChange}
                  className="input input-bordered w-full"
                  required
                />
              </div>
              <select
                name="schoolYearId"
                className="select select-bordered w-full"
                value={form.schoolYearId}
                onChange={handleChange}
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

export default AddSemesterModal;
