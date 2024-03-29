﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.Services.Entities
{
	/// <summary>
	/// Assign a person to Course
	/// </summary>
	class CourseStudent
	{
		/// <summary>
		/// Unique Identifyer
		/// </summary>
		public int ID { get; set; }

		/// <summary>
		/// Id of the course
		/// </summary>
		[Required]
		public int CourseID { get; set; }

		/// <summary>
		/// Id of the person
		/// </summary>
		[Required]
		public int PersonID { get; set; }

		/// <summary>
		/// 1 if student is active in the course
		/// 0 if student is not in the course anymore
		/// </summary>
		[Required]
		public int Active { get; set; }

	}
}
