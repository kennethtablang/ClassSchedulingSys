import { FaBars, FaBell, FaUserCircle } from "react-icons/fa";
import { Link, useNavigate } from "react-router-dom";
import { logoutUser } from "../../services/authService"; // adjust the path if needed

const Navbar = ({ toggleSidebar }) => {
  const navigate = useNavigate();

  const handleLogout = () => {
    logoutUser(); // Clears the token
    navigate("/login"); // Redirect to login page
  };

  return (
    <header className="sticky top-0 z-30 bg-white shadow-md px-6 py-4 flex justify-between items-center">
      {/* Left section: Hamburger + Title */}
      <div className="flex items-center gap-4">
        <button
          onClick={toggleSidebar}
          className="btn btn-ghost btn-sm md:hidden"
          aria-label="Toggle Sidebar"
        >
          <FaBars className="text-xl" />
        </button>
        <h2 className="text-lg font-semibold text-primary">Dashboard</h2>
      </div>

      {/* Right section: Notifications + Profile */}
      <div className="flex items-center gap-4">
        <button className="btn btn-ghost btn-sm relative">
          <FaBell className="text-lg" />
          <span className="absolute top-0 right-0 w-2 h-2 bg-red-500 rounded-full"></span>
        </button>

        {/* User dropdown */}
        <div className="dropdown dropdown-end">
          <label tabIndex={0} className="btn btn-ghost btn-sm avatar">
            <div className="w-8 rounded-full bg-gray-200 flex items-center justify-center">
              <FaUserCircle className="text-xl text-gray-600" />
            </div>
          </label>
          <ul
            tabIndex={0}
            className="mt-3 p-2 shadow menu menu-sm dropdown-content bg-base-100 rounded-box w-48"
          >
            <li>
              <Link to="/dashboard/profile">My Profile</Link>
            </li>
            <li>
              <a>Settings</a>
            </li>
            <li>
              <button
                onClick={handleLogout}
                className="text-error text-left w-full"
              >
                Logout
              </button>
            </li>
          </ul>
        </div>
      </div>
    </header>
  );
};

export default Navbar;
