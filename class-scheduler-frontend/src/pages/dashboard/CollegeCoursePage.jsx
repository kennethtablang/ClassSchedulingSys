import { useEffect, useState } from "react";
import {
  getCollegeCourses,
  deleteCollegeCourse,
} from "../../services/collegeCourseService";
import AddCollegeCourseModal from "../../components/collegecourse/AddCollegeCourseModal";
import EditCollegeCourseModal from "../../components/collegecourse/EditCollegeCourseModal";
import { FaEdit, FaTrash } from "react-icons/fa";
import { toast } from "react-toastify";

const CollegeCoursePage = () => {
  const [courses, setCourses] = useState([]);
  const [editing, setEditing] = useState(null);
  const [reload, setReload] = useState(false);

  const fetchCourses = async () => {
    try {
      const { data } = await getCollegeCourses();
      setCourses(data);
    } catch {
      toast.error("Failed to load courses.");
    }
  };

  useEffect(() => {
    fetchCourses();
  }, [reload]);

  const handleDelete = async (id) => {
    if (!confirm("Delete this college course?")) return;
    try {
      await deleteCollegeCourse(id);
      toast.success("Deleted.");
      setReload((r) => !r);
    } catch {
      toast.error("Failed to delete.");
    }
  };

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-2xl font-semibold">College Courses</h2>
        <AddCollegeCourseModal onSuccess={() => setReload((r) => !r)} />
      </div>

      <div className="overflow-x-auto bg-white rounded shadow">
        <table className="table w-full">
          <thead>
            <tr className="bg-gray-100 text-gray-700 text-sm">
              <th>Code</th>
              <th>Name</th>
              <th className="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {courses.length === 0 ? (
              <tr>
                <td colSpan="3" className="text-center py-4">
                  No college courses found.
                </td>
              </tr>
            ) : (
              courses.map((course) => (
                <tr key={course.id} className="border-b">
                  <td>{course.code}</td>
                  <td>{course.name}</td>
                  <td className="text-right space-x-2">
                    <button
                      className="btn btn-sm btn-warning"
                      onClick={() => setEditing(course)}
                    >
                      <FaEdit className="mr-1" /> Edit
                    </button>
                    <button
                      className="btn btn-sm btn-error"
                      onClick={() => handleDelete(course.id)}
                    >
                      <FaTrash className="mr-1" /> Delete
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {editing && (
        <EditCollegeCourseModal
          course={editing}
          onClose={() => setEditing(null)}
          onSuccess={() => {
            setEditing(null);
            setReload((r) => !r);
          }}
        />
      )}
    </div>
  );
};

export default CollegeCoursePage;
