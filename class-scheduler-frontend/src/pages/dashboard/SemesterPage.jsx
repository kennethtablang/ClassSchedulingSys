import { useEffect, useState } from "react";
import {
  getSemesters,
  deleteSemester,
  setSemesterAsCurrent,
} from "../../services/semesterService";
import AddSemesterModal from "../../components/semester/AddSemesterModal";
import EditSemesterModal from "../../components/semester/EditSemesterModal";
import { FaEdit, FaTrash } from "react-icons/fa";
import { toast } from "react-toastify";

const SemesterPage = () => {
  const [semesters, setSemesters] = useState([]);
  const [reload, setReload] = useState(false);
  const [editing, setEditing] = useState(null);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const fetchSemesters = async () => {
    try {
      const { data } = await getSemesters();
      setSemesters(data);
    } catch {
      toast.error("Failed to load semesters.");
    }
  };

  useEffect(() => {
    fetchSemesters();
  }, [reload]);

  const handleDelete = async (id) => {
    if (!confirm("Are you sure you want to delete this semester?")) return;
    try {
      await deleteSemester(id);
      toast.success("Semester deleted.");
      setReload((r) => !r);
    } catch {
      toast.error("Failed to delete semester.");
    }
  };

  const handleSetAsCurrent = async (id) => {
    try {
      await setSemesterAsCurrent(id);
      toast.success("Semester set as current.");
      setReload((r) => !r);
    } catch {
      toast.error("Failed to set semester as current.");
    }
  };

  const filtered = semesters.filter(
    (s) =>
      s.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      s.schoolYearLabel.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const totalPages = Math.ceil(filtered.length / itemsPerPage);
  const paginated = filtered.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-semibold">Semester Management</h2>
        <AddSemesterModal onSuccess={() => setReload((r) => !r)} />
      </div>

      {/* Search Bar */}
      <input
        type="text"
        className="input input-bordered mb-4 w-full md:w-1/3"
        placeholder="Search by name or school year"
        value={searchTerm}
        onChange={(e) => {
          setSearchTerm(e.target.value);
          setCurrentPage(1);
        }}
      />

      <div className="overflow-x-auto bg-white shadow rounded-lg">
        <table className="table w-full">
          <thead>
            <tr className="bg-gray-100 text-sm text-gray-700">
              <th>Name</th>
              <th>Start</th>
              <th>End</th>
              <th>School Year</th>
              <th>Status</th>
              <th className="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {paginated.length === 0 ? (
              <tr>
                <td colSpan="6" className="text-center py-4">
                  No semesters found.
                </td>
              </tr>
            ) : (
              paginated.map((sem) => (
                <tr key={sem.id} className="border-b">
                  <td>{sem.name}</td>
                  <td>{new Date(sem.startDate).toLocaleDateString()}</td>
                  <td>{new Date(sem.endDate).toLocaleDateString()}</td>
                  <td>{sem.schoolYearLabel}</td>
                  <td>
                    {sem.isCurrent ? (
                      <span className="badge badge-success">Current</span>
                    ) : (
                      <button
                        className="btn btn-xs btn-outline"
                        onClick={() => handleSetAsCurrent(sem.id)}
                        disabled={!sem.isSchoolYearCurrent}
                      >
                        Set as Current
                      </button>
                    )}
                  </td>
                  <td className="text-right space-x-2">
                    <button
                      className="btn btn-sm btn-warning"
                      onClick={() => setEditing(sem)}
                    >
                      <FaEdit className="mr-1" /> Edit
                    </button>
                    <button
                      className="btn btn-sm btn-error"
                      onClick={() => handleDelete(sem.id)}
                      disabled={sem.isCurrent} // Optional: prevent deleting current
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

      {/* Pagination Controls */}
      {totalPages > 1 && (
        <div className="flex justify-center mt-4 gap-2">
          <button
            className="btn btn-sm"
            onClick={() => setCurrentPage((prev) => Math.max(prev - 1, 1))}
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
            onClick={() =>
              setCurrentPage((prev) => Math.min(prev + 1, totalPages))
            }
            disabled={currentPage === totalPages}
          >
            Next
          </button>
        </div>
      )}

      {/* Edit Modal */}
      {editing && (
        <EditSemesterModal
          semester={editing}
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

export default SemesterPage;
