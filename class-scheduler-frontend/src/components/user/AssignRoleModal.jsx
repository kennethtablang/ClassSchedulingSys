// src/components/user/AssignRoleModal.jsx
import { useState, useEffect } from "react";
import { assignRole } from "../../services/userService";

const ROLES = ["SuperAdmin", "Dean", "Faculty"];

const AssignRoleModal = ({ user, onClose, onSuccess }) => {
  const [selectedRole, setSelectedRole] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (user && user.roles && user.roles.length > 0) {
      setSelectedRole(user.roles[0]); // assuming single-role users
    }
  }, [user]);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      await assignRole(user.id, selectedRole);
      onSuccess(); // refresh user list
      onClose(); // close modal
    } catch (err) {
      setError(err?.response?.data || "Failed to assign role.");
    } finally {
      setLoading(false);
    }
  };

  if (!user) return null;

  return (
    <dialog className="modal modal-open">
      <div className="modal-box w-full max-w-md">
        <h3 className="font-bold text-lg mb-4">Assign Role</h3>

        {error && <div className="alert alert-error text-sm">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">
              Current Role:
            </label>
            <p className="text-gray-700 mb-2">
              {user.roles?.[0] || "None assigned"}
            </p>

            <select
              className="select select-bordered w-full"
              value={selectedRole}
              onChange={(e) => setSelectedRole(e.target.value)}
              required
            >
              <option disabled value="">
                Select a role
              </option>
              {ROLES.map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
          </div>

          <div className="modal-action">
            <button
              type="button"
              className="btn"
              onClick={onClose}
              disabled={loading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className={`btn btn-primary ${loading ? "loading" : ""}`}
              disabled={loading}
            >
              Save Role
            </button>
          </div>
        </form>
      </div>
    </dialog>
  );
};

export default AssignRoleModal;
