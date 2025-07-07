import { useEffect, useState } from "react";
import { createClassSection } from "../../services/classSectionService";
import { getCollegeCourses } from "../../services/collegeCourseService";
import { getSemesters } from "../../services/semesterService";
import { toast } from "react-toastify";

const AddClassSectionModal = ({ onSuccess }) => {
  const [open, setOpen] = useState(false);
  const [section, setSection] = useState("");
  const [yearLevel, setYearLevel] = useState(1);
  const [collegeCourseId, setCollegeCourseId] = useState("");
  const [semesterId, setSemesterId] = useState("");
  const [courses, setCourses] = useState([]);
  const [semesters, setSemesters] = useState([]);

  const resetForm = () => {
    setSection("");
    setYearLevel(1);
    setCollegeCourseId("");
    setSemesterId("");
  };

  const fetchDropdowns = async () => {
    try {
      const [courseRes, semesterRes] = await Promise.all([
        getCollegeCourses(),
        getSemesters(),
      ]);
      setCourses(courseRes.data);
      setSemesters(semesterRes.data);
    } catch {
      toast.error("Failed to load dropdown data.");
    }
  };

  const handleSubmit = async () => {
    if (!section || !collegeCourseId || !semesterId) {
      toast.error("Please fill in all required fields.");
      return;
    }

    try {
      await createClassSection({
        section,
        yearLevel,
        collegeCourseId,
        semesterId,
      });
      toast.success("Section added successfully.");
      setOpen(false);
      resetForm();
      onSuccess();
    } catch {
      toast.error("Failed to create section.");
    }
  };

  useEffect(() => {
    if (open) fetchDropdowns();
  }, [open]);

  return (
    <>
      <button className="btn btn-primary" onClick={() => setOpen(true)}>
        Add Section
      </button>

      {open && (
        <dialog className="modal modal-open">
          <div className="modal-box max-w-xl">
            <h3 className="font-bold text-lg mb-4">Add Class Section</h3>

            <div className="space-y-4">
              <div>
                <label className="label font-semibold">Section Label</label>
                <input
                  type="text"
                  className="input input-bordered w-full"
                  value={section}
                  onChange={(e) => setSection(e.target.value)}
                  placeholder="Enter section (e.g., A, B)"
                />
              </div>

              <div>
                <label className="label font-semibold">Year Level</label>
                <select
                  className="select select-bordered w-full"
                  value={yearLevel}
                  onChange={(e) => setYearLevel(Number(e.target.value))}
                >
                  {[1, 2, 3, 4].map((year) => (
                    <option key={year} value={year}>
                      {year} Year
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="label font-semibold">Course</label>
                <select
                  className="select select-bordered w-full"
                  value={collegeCourseId}
                  onChange={(e) => setCollegeCourseId(e.target.value)}
                >
                  <option value="">Select a course</option>
                  {courses.map((course) => (
                    <option key={course.id} value={course.id}>
                      {course.code} - {course.name}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="label font-semibold">Semester</label>
                <select
                  className="select select-bordered w-full"
                  value={semesterId}
                  onChange={(e) => setSemesterId(e.target.value)}
                >
                  <option value="">Select a semester</option>
                  {semesters.map((sem) => (
                    <option key={sem.id} value={sem.id}>
                      {sem.name} ({sem.schoolYearLabel})
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="modal-action mt-6">
              <button className="btn btn-success" onClick={handleSubmit}>
                Save
              </button>
              <button
                className="btn btn-outline"
                onClick={() => setOpen(false)}
              >
                Cancel
              </button>
            </div>
          </div>
        </dialog>
      )}
    </>
  );
};

export default AddClassSectionModal;
