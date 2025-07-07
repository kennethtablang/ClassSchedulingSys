// src/components/dashboard/Sidebar.jsx
import { Link, useLocation } from "react-router-dom";
import {
  FaCalendarAlt,
  FaUsers,
  FaChalkboardTeacher,
  FaDoorOpen,
  FaUniversity,
  FaBuilding,
  FaBook,
  FaBell,
  FaFileAlt,
  FaLayerGroup,
  FaClock,
  FaExclamationTriangle,
  FaUsersCog,
} from "react-icons/fa";

const Sidebar = ({ isOpen }) => {
  const location = useLocation();

  const isActive = (path) =>
    location.pathname.startsWith(path)
      ? "bg-primary/10 text-primary font-semibold"
      : "text-gray-700 hover:bg-gray-100";

  const navItemClass =
    "flex items-center gap-3 px-4 py-2 rounded-lg transition duration-200";

  return (
    <aside
      className={`
        fixed top-0 left-0 h-screen w-64 z-50 bg-white border-r
        transiouttion-transform duration-300 ease-in-
        ${isOpen ? "translate-x-0" : "-translate-x-full"}
        md:translate-x-0 md:static
      `}
    >
      <div className="flex flex-col h-full overflow-y-auto px-6 py-8">
        {/* Logo / Brand */}
        <div className="mb-12 shrink-0">
          <h1 className="text-2xl font-extrabold text-primary tracking-tight">
            PCNL Scheduler
          </h1>
          <p className="text-xs text-gray-500 mt-1">
            Academic Scheduling System
          </p>
        </div>

        {/* Navigation */}
        <nav className="flex flex-col gap-2 flex-1">
          {/* Grouped Navigation */}
          {[
            {
              label: "Dashboard",
              links: [
                {
                  to: "/dashboard",
                  icon: <FaCalendarAlt />,
                  label: "Dashboard",
                },
                {
                  to: "/dashboard/schedules",
                  icon: <FaCalendarAlt />,
                  label: "Schedules",
                },
              ],
            },
            {
              label: "Academic",
              links: [
                {
                  to: "/dashboard/school-year",
                  icon: <FaClock />,
                  label: "School Year",
                },
                {
                  to: "/dashboard/semester",
                  icon: <FaLayerGroup />,
                  label: "Semester",
                },
                {
                  to: "/dashboard/class-section",
                  icon: <FaUsers />,
                  label: "Class Section",
                },
                {
                  to: "/dashboard/subjects",
                  icon: <FaBook />,
                  label: "Subjects",
                },
              ],
            },

            {
              label: "Management",
              links: [
                {
                  to: "/dashboard/departments",
                  icon: <FaUniversity />,
                  label: "Departments",
                },
                {
                  to: "/dashboard/faculty",
                  icon: <FaChalkboardTeacher />,
                  label: "Faculty",
                },
                {
                  to: "/dashboard/userManagement",
                  icon: <FaUsersCog />,
                  label: "User Mgmt",
                },
                {
                  to: "/dashboard/courses",
                  icon: <FaBook />,
                  label: "College Courses",
                },
              ],
            },
            {
              label: "Infrastructure",
              links: [
                {
                  to: "/dashboard/buildings",
                  icon: <FaBuilding />,
                  label: "Buildings",
                },
                {
                  to: "/dashboard/rooms",
                  icon: <FaDoorOpen />,
                  label: "Rooms",
                },
              ],
            },
            {
              label: "Communication",
              links: [
                {
                  to: "/dashboard/notifications",
                  icon: <FaBell />,
                  label: "Notifications",
                },
                {
                  to: "/dashboard/alert-faculty",
                  icon: <FaExclamationTriangle />,
                  label: "Alert Faculty",
                },
              ],
            },
            {
              label: "Reports",
              links: [
                {
                  to: "/dashboard/reports",
                  icon: <FaFileAlt />,
                  label: "Reports",
                },
                {
                  to: "/dashboard/archives",
                  icon: <FaFileAlt />,
                  label: "Archives",
                },
              ],
            },
          ].map((section) => (
            <div key={section.label}>
              <span className="text-xs text-gray-500 uppercase mt-4 mb-1 px-2">
                {section.label}
              </span>
              {section.links.map(({ to, icon, label }) => (
                <Link
                  key={to}
                  to={to}
                  className={`${navItemClass} ${isActive(to)}`}
                >
                  {icon} {label}
                </Link>
              ))}
            </div>
          ))}
        </nav>

        {/* Footer */}
        <div className="shrink-0 pt-6 text-center text-xs text-gray-400">
          © {new Date().getFullYear()} PCNL Scheduler
        </div>
      </div>
    </aside>
  );
};

export default Sidebar;
