// src/components/user/EditUserModal.jsx
import { useState, useEffect } from "react";
import { updateUser } from "../../services/userService";

const EditUserModal = ({ user, onClose, onSuccess }) => {
  const [formData, setFormData] = useState({
    firstName: "",
    middleName: "",
    lastName: "",
    phoneNumber: "",
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (user) {
      setFormData({
        firstName: user.firstName || "",
        middleName: user.middleName || "",
        lastName: user.lastName || "",
        phoneNumber: user.phoneNumber || "",
      });
    }
  }, [user]);

  const handleChange = (e) => {
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    try {
      await updateUser(user.id, formData);
      onSuccess(); // refresh parent data
      onClose(); // close modal
    } catch (err) {
      setError(err?.response?.data || "Failed to update user.");
    } finally {
      setLoading(false);
    }
  };

  if (!user) return null;

  return (
    <dialog className="modal modal-open">
      <div className="modal-box w-full max-w-xl">
        <h3 className="font-bold text-lg mb-4">Edit User Info</h3>

        {error && <div className="alert alert-error text-sm">{error}</div>}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <input
              name="firstName"
              type="text"
              className="input input-bordered w-full"
              placeholder="First Name"
              value={formData.firstName}
              onChange={handleChange}
              required
            />
            <input
              name="middleName"
              type="text"
              className="input input-bordered w-full"
              placeholder="Middle Name"
              value={formData.middleName}
              onChange={handleChange}
            />
            <input
              name="lastName"
              type="text"
              className="input input-bordered w-full"
              placeholder="Last Name"
              value={formData.lastName}
              onChange={handleChange}
              required
            />
            <input
              name="phoneNumber"
              type="text"
              className="input input-bordered w-full"
              placeholder="Phone Number"
              value={formData.phoneNumber}
              onChange={handleChange}
            />
          </div>

          <div className="modal-action flex justify-end gap-2">
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
              Save Changes
            </button>
          </div>
        </form>
      </div>
    </dialog>
  );
};

export default EditUserModal;
