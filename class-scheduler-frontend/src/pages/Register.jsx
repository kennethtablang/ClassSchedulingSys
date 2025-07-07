import { useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { useNavigate, Link } from "react-router-dom";
import { useState } from "react";
import { registerUser } from "../services/authService";
import registerImage from "../assets/register-illustration.svg";

// Validation Schema
const schema = yup.object({
  firstName: yup.string().required("First name is required"),
  middleName: yup.string(), // optional
  lastName: yup.string().required("Last name is required"),
  email: yup.string().email("Invalid email").required("Email is required"),
  password: yup
    .string()
    .min(6, "Minimum 6 characters")
    .required("Password is required"),
  confirmPassword: yup
    .string()
    .oneOf([yup.ref("password")], "Passwords must match")
    .required("Confirm your password"),
});

const RegisterPage = () => {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({ resolver: yupResolver(schema) });

  const navigate = useNavigate();
  const [error, setError] = useState(null);

  const onSubmit = async (formData) => {
    try {
      setError(null);
      const { firstName, middleName, lastName, email, password } = formData;

      await registerUser({
        firstName,
        middleName: middleName || null,
        lastName,
        email,
        password,
        confirmPassword: formData.confirmPassword,
      });

      navigate("/login");
    } catch (err) {
      console.error("Registration error:", err.response?.data || err.message);
      setError(err.response?.data || "Registration failed. Please try again.");
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center px-4 bg-gradient-to-br from-white to-blue-50 animate-fade-in">
      <div className="max-w-4xl w-full grid md:grid-cols-2 bg-white rounded-2xl shadow-xl overflow-hidden">
        {/* Left Image */}
        <div className="hidden md:flex items-center justify-center p-6 animate-slide-in-left">
          <img
            src={registerImage}
            alt="Register Illustration"
            className="w-full h-full object-contain animate-floating"
          />
        </div>

        {/* Right Form */}
        <div className="p-10 animate-slide-in-right">
          <h2 className="text-3xl font-bold text-primary mb-4">
            Create Account
          </h2>
          <p className="text-gray-600 mb-4">
            Register to access the PCNL Class Scheduler platform.
          </p>

          {error && (
            <div className="text-sm text-center text-red-500 mb-4">{error}</div>
          )}

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
            {/* First Name */}
            <div>
              <label className="label">First Name</label>
              <input
                type="text"
                placeholder="Juan"
                {...register("firstName")}
                className="input input-bordered w-full"
              />
              {errors.firstName && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.firstName.message}
                </p>
              )}
            </div>

            {/* Middle Name (optional) */}
            <div>
              <label className="label">Middle Name (Optional)</label>
              <input
                type="text"
                placeholder="Santos"
                {...register("middleName")}
                className="input input-bordered w-full"
              />
            </div>

            {/* Last Name */}
            <div>
              <label className="label">Last Name</label>
              <input
                type="text"
                placeholder="Dela Cruz"
                {...register("lastName")}
                className="input input-bordered w-full"
              />
              {errors.lastName && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.lastName.message}
                </p>
              )}
            </div>

            {/* Email */}
            <div>
              <label className="label">Email</label>
              <input
                type="email"
                placeholder="you@example.com"
                {...register("email")}
                className="input input-bordered w-full"
              />
              {errors.email && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.email.message}
                </p>
              )}
            </div>

            {/* Password */}
            <div>
              <label className="label">Password</label>
              <input
                type="password"
                placeholder="••••••••"
                {...register("password")}
                className="input input-bordered w-full"
              />
              {errors.password && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.password.message}
                </p>
              )}
            </div>

            {/* Confirm Password */}
            <div>
              <label className="label">Confirm Password</label>
              <input
                type="password"
                placeholder="••••••••"
                {...register("confirmPassword")}
                className="input input-bordered w-full"
              />
              {errors.confirmPassword && (
                <p className="text-red-500 text-sm mt-1">
                  {errors.confirmPassword.message}
                </p>
              )}
            </div>

            <button
              type="submit"
              className="btn btn-primary w-full hover:scale-[1.01] transition-all"
            >
              Register
            </button>

            <p className="text-sm text-center text-gray-600 mt-2">
              Already have an account?{" "}
              <Link to="/login" className="text-primary font-medium">
                Login here
              </Link>
            </p>
          </form>
        </div>
      </div>
    </div>
  );
};

export default RegisterPage;
