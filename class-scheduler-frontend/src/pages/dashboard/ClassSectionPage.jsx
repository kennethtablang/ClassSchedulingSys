// src/pages/dashboard/ClassSectionPage.jsx
import { useEffect, useState } from "react";
import {
  getClassSections,
  deleteClassSection,
} from "../../services/classSectionService";
import AddClassSectionModal from "../../components/classection/AddClassSectionModal";
import EditClassSectionModal from "../../components/classection/EditClassSectionModal";
import { FaEdit, FaTrash } from "react-icons/fa";
import { toast } from "react-toastify";

const ClassSectionPage = () => {
  const [sections, setSections] = useState([]);
  const [editing, setEditing] = useState(null);
  const [reload, setReload] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const fetchData = async () => {
    try {
      const { data } = await getClassSections();
      setSections(data);
    } catch {
      toast.error("Failed to load sections.");
    }
  };

  useEffect(() => {
    fetchData();
  }, [reload]);

  const handleDelete = async (id) => {
    if (!confirm("Delete this section?")) return;
    try {
      await deleteClassSection(id);
      toast.success("Section deleted.");
      setReload((r) => !r);
    } catch {
      toast.error("Failed to delete section.");
    }
  };

  const filtered = sections.filter(
    (s) =>
      s.section?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      s.courseName?.toLowerCase().includes(searchTerm.toLowerCase()) ||
      s.semesterLabel?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const totalPages = Math.ceil(filtered.length / itemsPerPage);
  const paginated = filtered.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-4">
        <h2 className="text-2xl font-semibold">Class Sections</h2>
        <AddClassSectionModal onSuccess={() => setReload((r) => !r)} />
      </div>

      <input
        type="text"
        placeholder="Search for section"
        className="input input-bordered mb-4 w-full md:w-1/3"
        value={searchTerm}
        onChange={(e) => {
          setSearchTerm(e.target.value);
          setCurrentPage(1);
        }}
      />

      <div className="overflow-x-auto bg-white shadow rounded">
        <table className="table w-full">
          <thead>
            <tr className="bg-gray-100 text-sm text-gray-700">
              <th>Section</th>
              <th>Year Level</th>
              <th>Course</th>
              <th>Semester</th>
              <th className="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {paginated.length === 0 ? (
              <tr>
                <td colSpan="5" className="text-center py-4">
                  No sections found.
                </td>
              </tr>
            ) : (
              paginated.map((s) => (
                <tr key={s.id}>
                  <td>{s.section}</td>
                  <td>{s.yearLevel}</td>
                  <td>{s.collegeCourseName}</td>
                  <td>
                    {s.semesterName} ({s.schoolYearLabel})
                  </td>
                  <td className="text-right space-x-2">
                    <button
                      className="btn btn-sm btn-warning"
                      onClick={() => setEditing(s)}
                    >
                      <FaEdit className="mr-1" /> Edit
                    </button>
                    <button
                      className="btn btn-sm btn-error"
                      onClick={() => handleDelete(s.id)}
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

      {totalPages > 1 && (
        <div className="flex justify-center mt-4 gap-2">
          <button
            className="btn btn-sm"
            onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))}
            disabled={currentPage === 1}
          >
            Prev
          </button>
          {Array.from({ length: totalPages }, (_, i) => i + 1).map((page) => (
            <button
              key={page}
              onClick={() => setCurrentPage(page)}
              className={`btn btn-sm ${
                currentPage === page ? "btn-primary" : "btn-outline"
              }`}
            >
              {page}
            </button>
          ))}
          <button
            className="btn btn-sm"
            onClick={() => setCurrentPage((p) => Math.min(p + 1, totalPages))}
            disabled={currentPage === totalPages}
          >
            Next
          </button>
        </div>
      )}

      {editing && (
        <EditClassSectionModal
          section={editing}
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

export default ClassSectionPage;
