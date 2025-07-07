// src/components/dashboard/EditProfileModal.jsx
import { useForm } from "react-hook-form";
import { updateProfile } from "../../services/profileService";

const EditProfileModal = ({ user, onClose, onUpdated }) => {
  const { register, handleSubmit } = useForm({
    defaultValues: user,
  });

  const onSubmit = async (data) => {
    await updateProfile(data);
    onUpdated();
    onClose();
  };

  return (
    <dialog id="edit_profile_modal" className="modal modal-open">
      <div className="modal-box">
        <h3 className="font-bold text-lg">Edit Profile</h3>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3 mt-4">
          <input
            type="text"
            placeholder="First Name"
            {...register("firstName")}
            className="input input-bordered w-full"
          />
          <input
            type="text"
            placeholder="Middle Name"
            {...register("middleName")}
            className="input input-bordered w-full"
          />
          <input
            type="text"
            placeholder="Last Name"
            {...register("lastName")}
            className="input input-bordered w-full"
          />
          <input
            type="text"
            placeholder="Phone Number"
            {...register("phoneNumber")}
            className="input input-bordered w-full"
          />
          <div className="modal-action">
            <button type="button" onClick={onClose} className="btn">
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              Save
            </button>
          </div>
        </form>
      </div>
    </dialog>
  );
};

export default EditProfileModal;
