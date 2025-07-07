import { Link } from "react-router-dom";
import {
  FaFacebookF,
  FaTwitter,
  FaInstagram,
  FaLinkedinIn,
} from "react-icons/fa";
import heroImage from "../assets/hero-illustration.svg"; // Replace with a valid SVG path
import logoImage from "../assets/pcnl.svg"; // Replace with a valid SVG path

const Landing = () => {
  return (
    <div className="bg-base-100 text-base-content px-4 lg:px-[50px] font-sans max-w-7xl mx-auto">
      {/* Navbar */}
      <div className="navbar bg-white shadow-md py-4 px-8 rounded-xl mt-4 animate-fade-in-down">
        <div className="flex-1">
          <Link to="/" className="flex items-center space-x-2">
            <img src={logoImage} alt="PCNL Logo" className="h-10 w-10" />
            <span className="text-2xl font-extrabold text-primary tracking-wide">
              PCNL Class Scheduler
            </span>
          </Link>
        </div>
        <div className="space-x-4">
          <Link
            to="/login"
            className="btn btn-outline btn-primary transition-all duration-300 hover:scale-105"
          >
            Login
          </Link>
          <Link
            to="/register"
            className="btn btn-primary transition-all duration-300 hover:scale-105"
          >
            Register
          </Link>
        </div>
      </div>

      {/* Hero Section */}
      <div className="hero min-h-[85vh] bg-gradient-to-br from-blue-50 to-white rounded-xl my-10 p-10 animate-fade-in">
        <div className="hero-content flex-col lg:flex-row gap-12 items-center">
          <div className="text-left max-w-xl animate-slide-in-left">
            <h1 className="text-6xl font-extrabold text-primary leading-tight">
              Revolutionize Class Scheduling
            </h1>
            <p className="text-lg text-gray-700 leading-relaxed mt-4">
              The Philippine College of Northwestern Luzon's official scheduling
              tool. Intuitive. Organized. Efficient.
            </p>
            <Link
              to="/login"
              className="btn btn-accent btn-lg mt-6 transition-all duration-300 hover:shadow-xl"
            >
              Launch Scheduler
            </Link>
          </div>
          <img
            src={heroImage}
            alt="Hero Illustration"
            className="w-full max-w-md animate-floating drop-shadow-xl"
          />
        </div>
      </div>

      {/* About Section */}
      <section className="bg-white shadow rounded-xl py-20 px-10 mb-12 text-center animate-fade-in">
        <h2 className="text-4xl font-bold mb-6 text-primary">
          About the System
        </h2>
        <p className="max-w-3xl mx-auto text-lg leading-relaxed text-gray-600">
          Built with academic excellence in mind, our system transforms
          scheduling into a smooth digital experience. Designed to eliminate
          overlaps, ease faculty assignments, and give students real-time access
          to their timetables.
        </p>
      </section>

      {/* Vision & Mission */}
      <section className="grid md:grid-cols-2 gap-12 py-20 animate-fade-in-up">
        <div className="bg-blue-50 rounded-xl p-10 shadow transition-all duration-300 hover:shadow-md">
          <h3 className="text-2xl font-bold mb-4 text-center text-primary">
            Vision
          </h3>
          <p className="text-center text-gray-700 leading-relaxed">
            A future-forward academic environment where every class, faculty,
            and student is synced in harmony through technology.
          </p>
        </div>
        <div className="bg-blue-50 rounded-xl p-10 shadow transition-all duration-300 hover:shadow-md">
          <h3 className="text-2xl font-bold mb-4 text-center text-primary">
            Mission
          </h3>
          <p className="text-center text-gray-700 leading-relaxed">
            To deliver a reliable scheduling platform that aligns with PCNL's
            goals for quality and innovation in education.
          </p>
        </div>
      </section>

      {/* Philosophy */}
      <section className="bg-white shadow-sm rounded-xl py-16 px-10 mb-10 text-center animate-fade-in">
        <h3 className="text-2xl font-bold mb-4 text-primary">Our Philosophy</h3>
        <p className="max-w-3xl mx-auto text-gray-600 leading-relaxed">
          Simplicity drives adoption. Accessibility drives equity. We believe
          scheduling should be seamless for all — from administrators to
          first-year students.
        </p>
      </section>

      {/* Features Section */}
      <section className="bg-blue-50 rounded-xl py-20 px-10 mb-10 animate-fade-in-up">
        <h3 className="text-2xl font-bold text-primary text-center mb-12">
          Why Choose This System?
        </h3>
        <div className="grid md:grid-cols-3 gap-10 text-center">
          <div className="p-6 bg-white rounded-lg shadow hover:shadow-xl transition-transform duration-300 hover:scale-105">
            <h4 className="font-bold text-primary mb-3 text-lg">
              Smart Conflict Detection
            </h4>
            <p className="text-gray-600">
              Avoid double-bookings and overlapping schedules effortlessly.
            </p>
          </div>
          <div className="p-6 bg-white rounded-lg shadow hover:shadow-xl transition-transform duration-300 hover:scale-105">
            <h4 className="font-bold text-primary mb-3 text-lg">
              Interactive Calendar
            </h4>
            <p className="text-gray-600">
              Navigate your weekly schedule with drag-and-drop clarity.
            </p>
          </div>
          <div className="p-6 bg-white rounded-lg shadow hover:shadow-xl transition-transform duration-300 hover:scale-105">
            <h4 className="font-bold text-primary mb-3 text-lg">
              Faculty Coordination
            </h4>
            <p className="text-gray-600">
              Assign classes efficiently, respecting professor availability and
              preferences.
            </p>
          </div>
        </div>
      </section>

      {/* Contact Section */}
      <section className="bg-base-100 rounded-xl py-16 px-10 mb-12 text-center animate-fade-in">
        <h3 className="text-2xl font-bold mb-6 text-primary">Contact Us</h3>
        <p className="text-gray-600 mb-2">📧 info@pcnl.edu.ph</p>
        <p className="text-gray-600 mb-2">📞 +63 912 345 6789</p>
        <p className="text-gray-600">
          📍 San Fernando City, La Union, Philippines
        </p>
      </section>

      {/* Footer */}
      <footer className="footer items-center p-6 bg-white border-t border-gray-200 justify-between text-base-content animate-fade-in-up">
        <aside className="ml-auto">
          <p className="text-sm">
            © 2025 Philippine College of Northwestern Luzon. All rights
            reserved.
          </p>
        </aside>
        <nav className="flex gap-4">
          <a href="#" aria-label="Facebook">
            <FaFacebookF className="w-5 h-5 text-primary transition-transform duration-300 hover:scale-125" />
          </a>
          <a href="#" aria-label="Twitter">
            <FaTwitter className="w-5 h-5 text-primary transition-transform duration-300 hover:scale-125" />
          </a>
          <a href="#" aria-label="Instagram">
            <FaInstagram className="w-5 h-5 text-primary transition-transform duration-300 hover:scale-125" />
          </a>
          <a href="#" aria-label="LinkedIn">
            <FaLinkedinIn className="w-5 h-5 text-primary transition-transform duration-300 hover:scale-125" />
          </a>
        </nav>
      </footer>
    </div>
  );
};

export default Landing;
