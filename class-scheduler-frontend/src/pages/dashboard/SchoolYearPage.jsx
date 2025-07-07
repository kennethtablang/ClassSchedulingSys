import { useEffect, useState } from "react";
import {
  getSchoolYears,
  deleteSchoolYear,
  setCurrentSchoolYear,
} from "../../services/schoolYearService";
import AddSchoolYearModal from "../../components/schoolyear/AddSchoolYearModal";
import EditSchoolYearModal from "../../components/schoolyear/EditSchoolYearModal";
import { toast } from "react-toastify";

const SchoolYearPage = () => {
  const [years, setYears] = useState([]);
  const [editing, setEditing] = useState(null);
  const [reload, setReload] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;

  const fetchData = async () => {
    try {
      const { data } = await getSchoolYears();
      setYears(data);
    } catch {
      toast.error("Failed to load school years.");
    }
  };

  useEffect(() => {
    fetchData();
  }, [reload]);

  const handleDelete = async (id) => {
    if (!confirm("Delete this school year?")) return;
    try {
      await deleteSchoolYear(id);
      toast.success("Deleted.");
      setReload((r) => !r);
    } catch {
      toast.error("Failed to delete.");
    }
  };

  const handleSetCurrent = async (id) => {
    try {
      await setCurrentSchoolYear(id);
      toast.success("Set as current year.");
      setReload((r) => !r);
    } catch {
      toast.error("Failed to set current.");
    }
  };

  const filtered = years.filter(
    (y) =>
      `${y.startYear}-${y.endYear}`.includes(searchTerm) ||
      (y.isCurrent && "current".includes(searchTerm.toLowerCase())) ||
      (y.isArchived && "archived".includes(searchTerm.toLowerCase()))
  );

  const totalPages = Math.ceil(filtered.length / itemsPerPage);
  const paginated = filtered.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage
  );

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-semibold">School Year Management</h2>
        <AddSchoolYearModal onSuccess={() => setReload((r) => !r)} />
      </div>

      {/* Search bar */}
      <input
        type="text"
        className="input input-bordered mb-4 w-full md:w-1/3"
        placeholder="Search by year or status"
        value={searchTerm}
        onChange={(e) => {
          setSearchTerm(e.target.value);
          setCurrentPage(1);
        }}
      />

      <div className="overflow-x-auto bg-white rounded shadow">
        <table className="table w-full">
          <thead>
            <tr className="bg-gray-100 text-sm text-gray-700">
              <th>Start</th>
              <th>End</th>
              <th>Status</th>
              <th className="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {paginated.length === 0 ? (
              <tr>
                <td colSpan="4" className="text-center py-4">
                  No school years found.
                </td>
              </tr>
            ) : (
              paginated.map((y) => (
                <tr key={y.id} className="border-b">
                  <td>{y.startYear}</td>
                  <td>{y.endYear}</td>
                  <td>
                    {y.isCurrent ? (
                      <span className="badge badge-success">Current</span>
                    ) : y.isArchived ? (
                      <span className="badge badge-ghost">Archived</span>
                    ) : (
                      <span className="badge badge-neutral">Inactive</span>
                    )}
                  </td>
                  <td className="text-right space-x-2">
                    <button
                      className="btn btn-sm btn-warning"
                      onClick={() => setEditing(y)}
                    >
                      Edit
                    </button>
                    <button
                      className="btn btn-sm btn-error"
                      onClick={() => handleDelete(y.id)}
                      disabled={y.isCurrent}
                    >
                      Delete
                    </button>
                    {!y.isCurrent && (
                      <button
                        className="btn btn-sm btn-outline"
                        onClick={() => handleSetCurrent(y.id)}
                        disabled={y.isArchived}
                      >
                        Set Current
                      </button>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
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

      {/* Edit modal */}
      {editing && (
        <EditSchoolYearModal
          year={editing}
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

export default SchoolYearPage;
