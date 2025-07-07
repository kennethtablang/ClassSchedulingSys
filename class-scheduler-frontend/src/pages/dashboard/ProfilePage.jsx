// src/pages/dashboard/ProfilePage.jsx
import { useEffect, useState } from "react";
import { getProfile } from "../../services/profileService";
import EditProfileModal from "../../components/dashboard/EditProfileModal";

const ProfilePage = () => {
  const [user, setUser] = useState(null);
  const [isEditing, setEditing] = useState(false);

  const fetchProfile = async () => {
    try {
      const data = await getProfile();
      setUser(data);
    } catch (err) {
      console.error("Failed to fetch profile", err);
    }
  };

  useEffect(() => {
    fetchProfile();
  }, []);

  if (!user) return <div className="p-4">Loading...</div>;

  return (
    <div className="p-6">
      <h2 className="text-2xl font-semibold mb-4">My Profile</h2>
      <div className="card shadow-md bg-base-100 p-6 space-y-3 max-w-lg">
        <div>
          <strong>Full Name:</strong> {user.fullName}
        </div>
        <div>
          <strong>Email:</strong> {user.email}
        </div>
        <div>
          <strong>Phone:</strong> {user.phoneNumber || "—"}
        </div>
        <div>
          <strong>Department:</strong> {user.departmentId || "—"}
        </div>
        <div className="pt-4">
          <button
            onClick={() => setEditing(true)}
            className="btn btn-primary btn-sm"
          >
            Edit Profile
          </button>
        </div>
      </div>

      {isEditing && (
        <EditProfileModal
          user={user}
          onClose={() => setEditing(false)}
          onUpdated={fetchProfile}
        />
      )}
    </div>
  );
};

export default ProfilePage;
